package com.capicua.gamecontroller.presentation.settings

import android.net.Uri
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.capicua.gamecontroller.data.config.AppConfig
import com.capicua.gamecontroller.data.config.ConfigBackup
import com.capicua.gamecontroller.data.config.ConfigDataStore
import com.capicua.gamecontroller.data.remote.ApiClient
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed class SettingsUiState {
    object Idle : SettingsUiState()
    object Probando : SettingsUiState()
    object Guardado : SettingsUiState()
    object ConexionOk : SettingsUiState()
    data class ConexionError(val mensaje: String) : SettingsUiState()
    object Exportado : SettingsUiState()
    /** La configuración importada ya está guardada; el fragment solo repinta el formulario. */
    data class Importado(val config: AppConfig) : SettingsUiState()
    data class ErrorArchivo(val mensaje: String) : SettingsUiState()
}

@HiltViewModel
class SettingsViewModel @Inject constructor(
    private val configDataStore: ConfigDataStore,
    private val apiClient: ApiClient,
    private val configBackup: ConfigBackup
) : ViewModel() {

    val config = configDataStore.config

    private val _estado = MutableStateFlow<SettingsUiState>(SettingsUiState.Idle)
    val estado: StateFlow<SettingsUiState> = _estado

    fun guardar(config: AppConfig) {
        viewModelScope.launch {
            configDataStore.guardar(config)
            _estado.value = SettingsUiState.Guardado
        }
    }

    fun probarConexion(config: AppConfig) {
        viewModelScope.launch(Dispatchers.IO) {
            _estado.value = SettingsUiState.Probando
            val resultado = apiClient.ejecutar(config, "SubirVolumen")
            _estado.value = resultado.fold(
                onSuccess = { SettingsUiState.ConexionOk },
                onFailure = { SettingsUiState.ConexionError(it.message ?: "Error desconocido") }
            )
        }
    }

    fun exportar(destino: Uri, config: AppConfig) {
        viewModelScope.launch {
            _estado.value = configBackup.exportar(destino, config).fold(
                onSuccess = { SettingsUiState.Exportado },
                onFailure = { SettingsUiState.ErrorArchivo(it.message ?: "No se pudo exportar") }
            )
        }
    }

    /** Importa y guarda de una vez: así lo importado sobrevive aunque se salga de Ajustes. */
    fun importar(origen: Uri) {
        viewModelScope.launch {
            _estado.value = configBackup.importar(origen).fold(
                onSuccess = { importada ->
                    configDataStore.guardar(importada)
                    SettingsUiState.Importado(importada)
                },
                onFailure = { SettingsUiState.ErrorArchivo(it.message ?: "No se pudo importar") }
            )
        }
    }

    fun limpiarEstado() {
        _estado.value = SettingsUiState.Idle
    }
}
