package com.capicua.gamecontroller.data.remote

import com.capicua.gamecontroller.data.config.AppConfig
import com.capicua.gamecontroller.presentation.log.LogEntrada
import okhttp3.Credentials
import okhttp3.OkHttpClient
import okhttp3.Request
import org.json.JSONObject
import timber.log.Timber
import java.security.SecureRandom
import java.security.cert.X509Certificate
import javax.inject.Inject
import javax.inject.Singleton
import javax.net.ssl.SSLContext
import javax.net.ssl.X509TrustManager

@Singleton
class ApiClient @Inject constructor(
    private val okHttpClient: OkHttpClient
) {

    private val unsafeClient: OkHttpClient by lazy {
        val trustAll = object : X509TrustManager {
            override fun checkClientTrusted(chain: Array<X509Certificate>, authType: String) {}
            override fun checkServerTrusted(chain: Array<X509Certificate>, authType: String) {}
            override fun getAcceptedIssuers(): Array<X509Certificate> = arrayOf()
        }
        val sslContext = SSLContext.getInstance("TLS").apply {
            init(null, arrayOf(trustAll), SecureRandom())
        }
        okHttpClient.newBuilder()
            .sslSocketFactory(sslContext.socketFactory, trustAll)
            .hostnameVerifier { _, _ -> true }
            .build()
    }

    private fun clientFor(config: AppConfig) = if (config.aceptarCertificadosInvalidos) unsafeClient else okHttpClient

    fun ejecutar(config: AppConfig, ruta: String): Result<Unit> {
        if (!config.configurado) {
            return Result.failure(IllegalStateException("Servidor no configurado"))
        }

        val url = "${config.urlServidor}/alexa/$ruta"
        val credenciales = Credentials.basic(config.usuario, config.contrasena)
        val request = Request.Builder()
            .url(url)
            .header("Authorization", credenciales)
            .get()
            .build()

        return try {
            val response = clientFor(config).newCall(request).execute()
            response.use {
                when {
                    it.isSuccessful -> {
                        Timber.d("Accion ejecutada: $ruta → ${it.code}")
                        Result.success(Unit)
                    }
                    it.code == 401 -> Result.failure(Exception("Credenciales incorrectas (401)"))
                    else -> Result.failure(Exception("Error del servidor: ${it.code}"))
                }
            }
        } catch (e: Exception) {
            Timber.e(e, "Error ejecutando accion: $ruta")
            Result.failure(e)
        }
    }

    fun obtenerLog(config: AppConfig): Result<List<LogEntrada>> {
        if (!config.configurado) {
            return Result.failure(IllegalStateException("Servidor no configurado"))
        }

        val url = "${config.urlServidor}/log/Obtener"
        val request = Request.Builder()
            .url(url)
            .header("Authorization", Credentials.basic(config.usuario, config.contrasena))
            .get()
            .build()

        return try {
            val response = clientFor(config).newCall(request).execute()
            response.use {
                when {
                    it.isSuccessful -> {
                        val body = it.body?.string() ?: "{}"
                        val json = JSONObject(body)
                        val arr = json.getJSONArray("entradas")
                        val entradas = mutableListOf<LogEntrada>()
                        for (i in 0 until arr.length()) {
                            val obj = arr.getJSONObject(i)
                            entradas.add(
                                LogEntrada(
                                    hora = obj.getString("hora"),
                                    nivel = obj.getString("nivel"),
                                    mensaje = obj.getString("mensaje"),
                                    excepcion = if (obj.has("excepcion")) obj.getString("excepcion") else null
                                )
                            )
                        }
                        Result.success(entradas)
                    }
                    it.code == 401 -> Result.failure(Exception("Credenciales incorrectas (401)"))
                    else -> Result.failure(Exception("Error del servidor: ${it.code}"))
                }
            }
        } catch (e: Exception) {
            Timber.e(e, "Error obteniendo log")
            Result.failure(e)
        }
    }

    fun borrarLog(config: AppConfig): Result<Unit> {
        if (!config.configurado) {
            return Result.failure(IllegalStateException("Servidor no configurado"))
        }

        val url = "${config.urlServidor}/log/Borrar"
        val request = Request.Builder()
            .url(url)
            .header("Authorization", Credentials.basic(config.usuario, config.contrasena))
            .delete()
            .build()

        return try {
            val response = clientFor(config).newCall(request).execute()
            response.use {
                when {
                    it.isSuccessful -> Result.success(Unit)
                    it.code == 401 -> Result.failure(Exception("Credenciales incorrectas (401)"))
                    else -> Result.failure(Exception("Error del servidor: ${it.code}"))
                }
            }
        } catch (e: Exception) {
            Timber.e(e, "Error borrando log")
            Result.failure(e)
        }
    }
}
