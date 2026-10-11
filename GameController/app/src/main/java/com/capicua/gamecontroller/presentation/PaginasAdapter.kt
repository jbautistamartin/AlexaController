package com.capicua.gamecontroller.presentation

import androidx.fragment.app.Fragment
import androidx.fragment.app.FragmentActivity
import androidx.viewpager2.adapter.FragmentStateAdapter
import com.capicua.gamecontroller.R
import com.capicua.gamecontroller.presentation.about.AboutFragment
import com.capicua.gamecontroller.presentation.home.HomeFragment
import com.capicua.gamecontroller.presentation.log.LogFragment
import com.capicua.gamecontroller.presentation.settings.SettingsFragment

/**
 * Las cuatro pestañas como páginas deslizables. El orden es el mismo que el del menú
 * inferior (`bottom_nav_menu.xml`): el id de cada opción hace de enlace entre ambos.
 */
class PaginasAdapter(activity: FragmentActivity) : FragmentStateAdapter(activity) {

    companion object {
        /** Id del menú inferior correspondiente a cada posición. */
        val PAGINAS = listOf(
            R.id.homeFragment,
            R.id.logFragment,
            R.id.settingsFragment,
            R.id.aboutFragment
        )

        fun posicionDe(idMenu: Int): Int = PAGINAS.indexOf(idMenu).coerceAtLeast(0)
    }

    override fun getItemCount(): Int = PAGINAS.size

    override fun createFragment(position: Int): Fragment = when (PAGINAS[position]) {
        R.id.logFragment -> LogFragment()
        R.id.settingsFragment -> SettingsFragment()
        R.id.aboutFragment -> AboutFragment()
        else -> HomeFragment()
    }
}
