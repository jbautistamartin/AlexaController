package com.capicua.gamecontroller.data.update

/**
 * Una versión `mayor.menor.parche` comparable **como números**.
 *
 * Comparar etiquetas como texto funciona hasta que un número pasa de nueve: alfabéticamente
 * `"1.10.0"` va antes de `"1.9.0"`, y eso en un comprobador de versiones significa dejar de
 * avisar a partir de la décima entrega.
 *
 * De `1.2.3-rc1+sha` se queda con `1.2.3`: los tags de este repositorio son `vX.Y.Z`.
 */
data class VersionPublicada(
    val mayor: Int,
    val menor: Int,
    val parche: Int
) : Comparable<VersionPublicada> {

    override fun compareTo(other: VersionPublicada): Int = when {
        mayor != other.mayor -> mayor - other.mayor
        menor != other.menor -> menor - other.menor
        else -> parche - other.parche
    }

    override fun toString(): String = "$mayor.$menor.$parche"

    companion object {
        /**
         * Interpreta `v1.2.0`, `1.2.0`, `1.2` o `1`; lo que falte vale cero.
         *
         * Devuelve null cuando no hay por dónde: una etiqueta que no se entiende no puede
         * decidir que hay una versión nueva.
         */
        fun interpretar(texto: String?): VersionPublicada? {
            if (texto.isNullOrBlank()) return null

            var limpia = texto.trim()
            if (limpia.startsWith("v", ignoreCase = true)) limpia = limpia.substring(1)

            val corte = limpia.indexOfFirst { it == '-' || it == '+' }
            if (corte > 0) limpia = limpia.substring(0, corte)

            val partes = limpia.split('.')
            if (partes.isEmpty() || partes.size > 3) return null

            val numeros = partes.map { it.toIntOrNull() ?: return null }
            if (numeros.any { it < 0 }) return null

            return VersionPublicada(
                mayor = numeros[0],
                menor = numeros.getOrElse(1) { 0 },
                parche = numeros.getOrElse(2) { 0 }
            )
        }
    }
}
