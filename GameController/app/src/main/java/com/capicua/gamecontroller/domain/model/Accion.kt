package com.capicua.gamecontroller.domain.model

enum class Accion(val ruta: String) {
    APAGAR_EQUIPO("ApagarEquipo"),
    REINICIAR_EQUIPO("ReiniciarEquipo"),
    INICIAR_STEAM("IniciarSteam"),
    CERRAR_STEAM("CerrarSteam"),
    REINICIAR_STEAM("ReiniciarSteam"),
    CERRAR_RETROARCH("CerrarRetroArch"),
    ENFOCAR_JUEGO("EnfocarJuego"),
    DETENER_JUEGO("DetenerJuego"),
    INICIAR_MODO_JUEGOS("IniciarModoJuegos"),
    DETENER_MODO_JUEGOS("DetenerModoJuegos"),
    SUBIR_VOLUMEN("SubirVolumen"),
    BAJAR_VOLUMEN("BajarVolumen"),
    RECONECTAR_MANDO("ReconectarMando"),
    SILENCIAR("Silenciar")
}
