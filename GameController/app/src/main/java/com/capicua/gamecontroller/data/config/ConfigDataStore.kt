package com.capicua.gamecontroller.data.config

import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.intPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ConfigDataStore @Inject constructor(
    private val dataStore: DataStore<Preferences>
) {

    companion object {
        private val KEY_URL = stringPreferencesKey("url_servidor")
        private val KEY_USUARIO = stringPreferencesKey("usuario")
        private val KEY_CONTRASENA = stringPreferencesKey("contrasena")
        private val KEY_CERT_INVALIDOS = booleanPreferencesKey("cert_invalidos")
        private val KEY_INTERVALO_LOG = intPreferencesKey("intervalo_refresco_log")
        private val KEY_IR_AL_LOG = booleanPreferencesKey("ir_al_log_al_ejecutar")
    }

    val config: Flow<AppConfig> = dataStore.data.map { prefs ->
        AppConfig(
            urlServidor = prefs[KEY_URL] ?: "",
            usuario = prefs[KEY_USUARIO] ?: "",
            contrasena = prefs[KEY_CONTRASENA] ?: "",
            aceptarCertificadosInvalidos = prefs[KEY_CERT_INVALIDOS] ?: false,
            intervaloRefrescoLog = prefs[KEY_INTERVALO_LOG] ?: 1,
            irAlLogAlEjecutar = prefs[KEY_IR_AL_LOG] ?: false
        )
    }

    suspend fun guardar(config: AppConfig) {
        dataStore.edit { prefs ->
            prefs[KEY_URL] = config.urlServidor.trimEnd('/')
            prefs[KEY_USUARIO] = config.usuario
            prefs[KEY_CONTRASENA] = config.contrasena
            prefs[KEY_CERT_INVALIDOS] = config.aceptarCertificadosInvalidos
            prefs[KEY_INTERVALO_LOG] = config.intervaloRefrescoLog.coerceAtLeast(1)
            prefs[KEY_IR_AL_LOG] = config.irAlLogAlEjecutar
        }
    }
}
