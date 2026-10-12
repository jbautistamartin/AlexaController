package com.capicua.gamecontroller

import android.os.Bundle
import androidx.activity.OnBackPressedCallback
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.ViewCompat
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.updatePadding
import androidx.viewpager2.widget.ViewPager2
import androidx.lifecycle.lifecycleScope
import com.capicua.gamecontroller.data.update.ComprobadorDeActualizaciones
import com.capicua.gamecontroller.data.update.ResultadoActualizacion
import com.capicua.gamecontroller.databinding.ActivityMainBinding
import com.capicua.gamecontroller.presentation.PaginasAdapter
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

@AndroidEntryPoint
class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding

    /**
     * El comprobador de versiones. Se pregunta aquí, y no solo desde «Acerca de», para que
     * enterarse de que hay una versión nueva no dependa de abrir esa pestaña.
     */
    @Inject
    lateinit var comprobadorDeActualizaciones: ComprobadorDeActualizaciones

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        configurarInsets()
        configurarPaginas()
        comprobarSiHayVersionNueva()
    }

    /**
     * Pregunta —una vez al día como mucho, y solo si el aviso está encendido— si hay publicada
     * una versión más nueva, y lo marca con un punto en la pestaña de «Acerca de», que es donde
     * está la tarjeta que lo explica.
     *
     * Un punto y nada más: el mando se abre para dar una orden al PC, y un diálogo encima al
     * arrancar estorbaría justo a eso. Sin red —el móvil puede estar fuera de casa— no se dice
     * nada.
     */
    private fun comprobarSiHayVersionNueva() {
        lifecycleScope.launch {
            val resultado = comprobadorDeActualizaciones.comprobar(forzar = false)
            if (resultado !is ResultadoActualizacion.Disponible) return@launch

            Timber.i("Versión nueva publicada: ${resultado.etiqueta}")
            binding.bottomNavigation.getOrCreateBadge(R.id.aboutFragment)
        }
    }

    /**
     * Las pestañas son páginas de un ViewPager2 (se cambian deslizando) y el menú inferior
     * es su reflejo: cada uno mueve al otro.
     */
    private fun configurarPaginas() {
        binding.pager.adapter = PaginasAdapter(this)

        binding.pager.registerOnPageChangeCallback(object : ViewPager2.OnPageChangeCallback() {
            override fun onPageSelected(position: Int) {
                binding.bottomNavigation.menu.getItem(position).isChecked = true
            }
        })

        binding.bottomNavigation.setOnItemSelectedListener { item ->
            // Sin animación al saltar varias páginas: el barrido intermedio marea.
            val destino = PaginasAdapter.posicionDe(item.itemId)
            val salto = kotlin.math.abs(destino - binding.pager.currentItem) > 1
            binding.pager.setCurrentItem(destino, !salto)
            true
        }

        // Atrás vuelve a Inicio en vez de cerrar la app, como hacía la navegación anterior.
        onBackPressedDispatcher.addCallback(this, object : OnBackPressedCallback(true) {
            override fun handleOnBackPressed() {
                if (binding.pager.currentItem == 0) {
                    isEnabled = false
                    onBackPressedDispatcher.onBackPressed()
                    isEnabled = true
                } else {
                    binding.pager.setCurrentItem(0, false)
                }
            }
        })
    }

    /**
     * La ventana se dibuja de borde a borde (obligatorio al apuntar a SDK 35), así que el
     * contenido hay que apartarlo a mano de la barra de estado, del notch y de la barra de
     * navegación. Los insets se consumen en la raíz para que ninguna vista hija —la
     * BottomNavigationView incluida— vuelva a aplicarlos por su cuenta.
     */
    private fun configurarInsets() {
        WindowCompat.setDecorFitsSystemWindows(window, false)
        WindowCompat.getInsetsController(window, binding.root).apply {
            // La paleta de la app es siempre clara: iconos de sistema oscuros.
            isAppearanceLightStatusBars = true
            isAppearanceLightNavigationBars = true
        }

        ViewCompat.setOnApplyWindowInsetsListener(binding.root) { _, insets ->
            val barras = insets.getInsets(
                WindowInsetsCompat.Type.systemBars() or WindowInsetsCompat.Type.displayCutout()
            )
            val teclado = insets.getInsets(WindowInsetsCompat.Type.ime())

            binding.root.updatePadding(
                left = barras.left,
                top = barras.top,
                right = barras.right,
                // Con el teclado abierto se sube todo el contenido por encima de él.
                bottom = (teclado.bottom - barras.bottom).coerceAtLeast(0)
            )
            // El fondo de la barra inferior tapa la zona del gesto de navegación.
            binding.bottomNavigation.updatePadding(bottom = barras.bottom)

            WindowInsetsCompat.CONSUMED
        }
    }
}
