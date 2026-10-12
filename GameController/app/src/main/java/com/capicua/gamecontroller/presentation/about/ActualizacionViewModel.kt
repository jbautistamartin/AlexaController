package com.capicua.gamecontroller.presentation.about

import android.content.Intent
import android.net.Uri
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.capicua.gamecontroller.data.update.ComprobadorDeActualizaciones
import com.capicua.gamecontroller.data.update.DescargaDeApk
import com.capicua.gamecontroller.data.update.EstadoActualizacion
import com.capicua.gamecontroller.data.update.ResultadoActualizacion
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

/**
 * El estado de la tarjeta de actualizaciones de la pestaña «Acerca de».
 *
 * Al crearse comprueba respetando el intervalo de un día, así que cambiar de pestaña no son
 * diez preguntas a GitHub; el botón sí fuerza, porque es una pregunta explícita.
 */
@HiltViewModel
class ActualizacionViewModel @Inject constructor(
    private val comprobador: ComprobadorDeActualizaciones,
    private val descargas: DescargaDeApk,
    private val estado: EstadoActualizacion
) : ViewModel() {

    data class Estado(
        val avisar: Boolean = true,
        val comprobando: Boolean = false,
        val resultado: ResultadoActualizacion? = null,
        val porcentaje: Int? = null,
        val error: String? = null,
        /** APK listo para instalar; la pantalla lo usa una vez y avisa. */
        val apkListo: Uri? = null
    )

    private val _ui = MutableStateFlow(Estado())
    val ui: StateFlow<Estado> = _ui.asStateFlow()

    private var descarga: Job? = null

    init {
        viewModelScope.launch {
            estado.avisar.collect { avisar -> _ui.update { it.copy(avisar = avisar) } }
        }
        comprobar(forzar = false)
    }

    fun comprobar(forzar: Boolean) {
        viewModelScope.launch {
            _ui.update { it.copy(comprobando = true, error = null) }
            val resultado = comprobador.comprobar(forzar)
            _ui.update { anterior ->
                anterior.copy(
                    comprobando = false,
                    // «No tocaba preguntar» no borra lo que ya se sabía.
                    resultado = if (resultado is ResultadoActualizacion.SinPreguntar) {
                        anterior.resultado
                    } else {
                        resultado
                    }
                )
            }
        }
    }

    fun cambiarAvisos(activado: Boolean) {
        viewModelScope.launch { estado.cambiarAvisos(activado) }
    }

    fun descargar() {
        val disponible = _ui.value.resultado as? ResultadoActualizacion.Disponible ?: return
        if (descarga?.isActive == true) return

        descarga = viewModelScope.launch {
            descargas.descargar(disponible).collect { paso ->
                when (paso) {
                    is DescargaDeApk.Paso.Progreso ->
                        _ui.update { it.copy(porcentaje = paso.porcentaje, error = null) }

                    is DescargaDeApk.Paso.Lista ->
                        _ui.update { it.copy(porcentaje = null, apkListo = paso.uri) }

                    is DescargaDeApk.Paso.Fallo ->
                        _ui.update { it.copy(porcentaje = null, error = paso.motivo) }
                }
            }
        }
    }

    /** La pantalla ya abrió el instalador: que no se abra otra vez al volver a la pestaña. */
    fun apkConsumido() {
        _ui.update { it.copy(apkListo = null) }
    }

    fun puedeInstalar(): Boolean = descargas.puedeInstalar()

    fun intentDeAutorizacion(): Intent = descargas.intentDeAutorizacion()

    fun intentDeInstalacion(uri: Uri): Intent = descargas.intentDeInstalacion(uri)
}
