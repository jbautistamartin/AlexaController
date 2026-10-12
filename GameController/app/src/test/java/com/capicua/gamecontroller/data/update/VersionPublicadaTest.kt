package com.capicua.gamecontroller.data.update

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Tests de la comparación de versiones del aviso de actualización.
 *
 * Son las primeras pruebas unitarias del mando, y están aquí porque esta es la única pieza del
 * comprobador que se puede probar sin red ni PC delante — y la que falla en silencio: si la
 * comparación se equivoca, la aplicación deja de avisar y nadie se entera hasta mirar los
 * releases a mano.
 */
class VersionPublicadaTest {

    @Test
    fun `interpreta la etiqueta con y sin la v del tag`() {
        assertEquals(VersionPublicada(1, 2, 0), VersionPublicada.interpretar("v1.2.0"))
        assertEquals(VersionPublicada(1, 2, 0), VersionPublicada.interpretar("1.2.0"))
    }

    @Test
    fun `lo que falta vale cero`() {
        assertEquals(VersionPublicada(2, 0, 0), VersionPublicada.interpretar("2"))
        assertEquals(VersionPublicada(2, 3, 0), VersionPublicada.interpretar("2.3"))
    }

    @Test
    fun `descarta el sufijo de preestreno y el de metadatos`() {
        assertEquals(VersionPublicada(1, 3, 0), VersionPublicada.interpretar("v1.3.0-rc2"))
        assertEquals(VersionPublicada(1, 3, 0), VersionPublicada.interpretar("1.3.0+abc1234"))
    }

    @Test
    fun `una etiqueta que no es una version no se interpreta`() {
        assertNull(VersionPublicada.interpretar(null))
        assertNull(VersionPublicada.interpretar(""))
        assertNull(VersionPublicada.interpretar("latest"))
        assertNull(VersionPublicada.interpretar("1.2.0.1"))
        assertNull(VersionPublicada.interpretar("1.dos.0"))
    }

    /**
     * El caso que justifica la clase: como texto, «1.10.0» va *antes* de «1.9.0», así que
     * comparar cadenas dejaría de avisar en cuanto la *minor* pasara de nueve.
     */
    @Test
    fun `la decima version es mayor que la novena`() {
        val diez = VersionPublicada.interpretar("1.10.0")!!
        val nueve = VersionPublicada.interpretar("1.9.0")!!

        assertTrue(diez > nueve)
    }

    @Test
    fun `la mayor pesa mas que la menor y que el parche`() {
        assertTrue(VersionPublicada(2, 0, 0) > VersionPublicada(1, 99, 99))
    }

    @Test
    fun `la version instalada no se supera a si misma`() {
        val instalada = VersionPublicada.interpretar("1.2.0")!!

        assertTrue(VersionPublicada.interpretar("v1.2.0")!! <= instalada)
        assertTrue(VersionPublicada.interpretar("1.1.9")!! < instalada)
        assertTrue(VersionPublicada.interpretar("v1.2.1")!! > instalada)
    }
}
