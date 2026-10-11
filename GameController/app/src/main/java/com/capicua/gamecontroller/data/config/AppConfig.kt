package com.capicua.gamecontroller.data.config

data class AppConfig(
    val urlServidor: String = "",
    val usuario: String = "",
    val contrasena: String = "",
    val aceptarCertificadosInvalidos: Boolean = false,
    val intervaloRefrescoLog: Int = 1,
    val irAlLogAlEjecutar: Boolean = false
) {
    val configurado: Boolean
        get() = urlServidor.isNotBlank() && usuario.isNotBlank() && contrasena.isNotBlank()
}
