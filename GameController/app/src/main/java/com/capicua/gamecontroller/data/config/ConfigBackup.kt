package com.capicua.gamecontroller.data.config

import android.content.Context
import android.net.Uri
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import timber.log.Timber
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Exporta e importa la configuración como JSON sobre un Uri del Storage Access Framework
 * (el usuario elige el archivo, así que no hace falta ningún permiso de almacenamiento).
 */
@Singleton
class ConfigBackup @Inject constructor(
    @ApplicationContext private val context: Context
) {

    companion object {
        /** Versión del formato: permite migrar si algún día cambian los campos. */
        const val VERSION = 1

        private const val CAMPO_VERSION = "version"
        private const val CAMPO_URL = "urlServidor"
        private const val CAMPO_USUARIO = "usuario"
        private const val CAMPO_CONTRASENA = "contrasena"
        private const val CAMPO_CERT_INVALIDOS = "aceptarCertificadosInvalidos"
        private const val CAMPO_INTERVALO_LOG = "intervaloRefrescoLog"
        private const val CAMPO_IR_AL_LOG = "irAlLogAlEjecutar"
    }

    suspend fun exportar(destino: Uri, config: AppConfig): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            // "wt" trunca: si el usuario sobrescribe un archivo mayor no quedan restos del anterior.
            context.contentResolver.openOutputStream(destino, "wt")?.use { salida ->
                salida.write(aJson(config).toByteArray())
            } ?: return@withContext Result.failure(Exception("No se pudo abrir el archivo de destino"))
            Result.success(Unit)
        } catch (e: Exception) {
            Timber.e(e, "Error exportando la configuracion")
            Result.failure(e)
        }
    }

    suspend fun importar(origen: Uri): Result<AppConfig> = withContext(Dispatchers.IO) {
        try {
            val texto = context.contentResolver.openInputStream(origen)?.use { entrada ->
                entrada.readBytes().decodeToString()
            } ?: return@withContext Result.failure(Exception("No se pudo abrir el archivo"))
            Result.success(desdeJson(texto))
        } catch (e: Exception) {
            Timber.e(e, "Error importando la configuracion")
            Result.failure(e)
        }
    }

    private fun aJson(config: AppConfig): String = JSONObject().apply {
        put(CAMPO_VERSION, VERSION)
        put(CAMPO_URL, config.urlServidor)
        put(CAMPO_USUARIO, config.usuario)
        put(CAMPO_CONTRASENA, config.contrasena)
        put(CAMPO_CERT_INVALIDOS, config.aceptarCertificadosInvalidos)
        put(CAMPO_INTERVALO_LOG, config.intervaloRefrescoLog)
        put(CAMPO_IR_AL_LOG, config.irAlLogAlEjecutar)
    }.toString(2)

    /** Lectura tolerante: los campos que falten se quedan con su valor por defecto. */
    private fun desdeJson(texto: String): AppConfig {
        val json = JSONObject(texto)
        if (!json.has(CAMPO_URL) && !json.has(CAMPO_USUARIO)) {
            throw IllegalArgumentException("El archivo no contiene una configuración de Game Controller")
        }
        val porDefecto = AppConfig()
        return AppConfig(
            urlServidor = json.optString(CAMPO_URL, porDefecto.urlServidor).trim(),
            usuario = json.optString(CAMPO_USUARIO, porDefecto.usuario).trim(),
            contrasena = json.optString(CAMPO_CONTRASENA, porDefecto.contrasena),
            aceptarCertificadosInvalidos =
                json.optBoolean(CAMPO_CERT_INVALIDOS, porDefecto.aceptarCertificadosInvalidos),
            intervaloRefrescoLog =
                json.optInt(CAMPO_INTERVALO_LOG, porDefecto.intervaloRefrescoLog).coerceAtLeast(1),
            irAlLogAlEjecutar = json.optBoolean(CAMPO_IR_AL_LOG, porDefecto.irAlLogAlEjecutar)
        )
    }
}
