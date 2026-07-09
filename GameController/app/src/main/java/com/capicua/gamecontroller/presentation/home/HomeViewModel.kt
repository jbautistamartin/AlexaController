package com.capicua.gamecontroller.presentation.home

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.capicua.gamecontroller.data.config.ConfigDataStore
import com.capicua.gamecontroller.data.remote.ApiClient
import com.capicua.gamecontroller.domain.model.Accion
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed class HomeUiState {
    object Idle : HomeUiState()
    object Cargando : HomeUiState()
    object Exito : HomeUiState()
    object ExitoIrAlLog : HomeUiState()
    data class Error(val mensaje: String) : HomeUiState()
}

@HiltViewModel
class HomeViewModel @Inject constructor(
    private val apiClient: ApiClient,
    private val configDataStore: ConfigDataStore
) : ViewModel() {

    private val _estado = MutableStateFlow<HomeUiState>(HomeUiState.Idle)
    val estado: StateFlow<HomeUiState> = _estado

    fun ejecutar(accion: Accion) {
        if (_estado.value is HomeUiState.Cargando) return

        viewModelScope.launch(Dispatchers.IO) {
            val config = configDataStore.config.first()
            if (!config.configurado) {
                _estado.value = HomeUiState.Error("Servidor no configurado. Ve a Ajustes.")
                return@launch
            }
            _estado.value = HomeUiState.Cargando
            val resultado = apiClient.ejecutar(config, accion.ruta)
            _estado.value = resultado.fold(
                onSuccess = {
                    if (config.irAlLogAlEjecutar) HomeUiState.ExitoIrAlLog
                    else HomeUiState.Exito
                },
                onFailure = { HomeUiState.Error(it.message ?: "Error desconocido") }
            )
        }
    }

    fun limpiarEstado() {
        _estado.value = HomeUiState.Idle
    }
}
