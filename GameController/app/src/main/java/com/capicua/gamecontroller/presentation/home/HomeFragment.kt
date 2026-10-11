package com.capicua.gamecontroller.presentation.home

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.capicua.gamecontroller.R
import com.capicua.gamecontroller.databinding.FragmentHomeBinding
import com.capicua.gamecontroller.domain.model.Accion
import com.google.android.material.bottomnavigation.BottomNavigationView
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.google.android.material.snackbar.Snackbar
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.launch

@AndroidEntryPoint
class HomeFragment : Fragment() {

    private var _binding: FragmentHomeBinding? = null
    private val binding get() = _binding!!
    private val viewModel: HomeViewModel by viewModels()

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentHomeBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        configurarBotones()
        observarEstado()
    }

    private fun confirmar(mensajeRes: Int, onConfirmar: () -> Unit) {
        MaterialAlertDialogBuilder(requireContext())
            .setTitle(R.string.confirmar_titulo)
            .setMessage(mensajeRes)
            .setPositiveButton(R.string.confirmar_si) { _, _ -> onConfirmar() }
            .setNegativeButton(R.string.confirmar_no, null)
            .show()
    }

    private fun configurarBotones() {
        with(binding) {
            btnApagar.setOnClickListener {
                confirmar(R.string.confirmar_apagar) { viewModel.ejecutar(Accion.APAGAR_EQUIPO) }
            }
            btnReiniciarEquipo.setOnClickListener {
                confirmar(R.string.confirmar_reiniciar_equipo) { viewModel.ejecutar(Accion.REINICIAR_EQUIPO) }
            }
            btnIniciarSteam.setOnClickListener { viewModel.ejecutar(Accion.INICIAR_STEAM) }
            btnCerrarSteam.setOnClickListener {
                confirmar(R.string.confirmar_cerrar_steam) { viewModel.ejecutar(Accion.CERRAR_STEAM) }
            }
            btnReiniciarSteam.setOnClickListener {
                confirmar(R.string.confirmar_reiniciar_steam) { viewModel.ejecutar(Accion.REINICIAR_STEAM) }
            }
            btnIniciarModoJuegos.setOnClickListener { viewModel.ejecutar(Accion.INICIAR_MODO_JUEGOS) }
            btnDetenerModoJuegos.setOnClickListener {
                confirmar(R.string.confirmar_detener_juegos) { viewModel.ejecutar(Accion.DETENER_MODO_JUEGOS) }
            }
            btnCerrarRetroarch.setOnClickListener { viewModel.ejecutar(Accion.CERRAR_RETROARCH) }
            btnEnfocarJuego.setOnClickListener { viewModel.ejecutar(Accion.ENFOCAR_JUEGO) }
            btnDetenerJuego.setOnClickListener {
                confirmar(R.string.confirmar_detener_juego) { viewModel.ejecutar(Accion.DETENER_JUEGO) }
            }
            btnReconectarMando.setOnClickListener { viewModel.ejecutar(Accion.RECONECTAR_MANDO) }
            btnSubirVolumen.setOnClickListener { viewModel.ejecutar(Accion.SUBIR_VOLUMEN) }
            btnBajarVolumen.setOnClickListener { viewModel.ejecutar(Accion.BAJAR_VOLUMEN) }
            btnSilenciar.setOnClickListener { viewModel.ejecutar(Accion.SILENCIAR) }
        }
    }

    private fun observarEstado() {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                viewModel.estado.collect { estado ->
                    actualizarUi(estado)
                }
            }
        }
    }

    private fun actualizarUi(estado: HomeUiState) {
        habilitarBotones(estado !is HomeUiState.Cargando)

        when (estado) {
            is HomeUiState.Exito -> {
                Snackbar.make(binding.root, R.string.comando_enviado, Snackbar.LENGTH_SHORT).show()
                viewModel.limpiarEstado()
            }
            is HomeUiState.ExitoIrAlLog -> {
                viewModel.limpiarEstado()
                requireActivity()
                    .findViewById<BottomNavigationView>(R.id.bottom_navigation)
                    .selectedItemId = R.id.logFragment
            }
            is HomeUiState.Error -> {
                Snackbar.make(
                    binding.root,
                    getString(R.string.error_conexion, estado.mensaje),
                    Snackbar.LENGTH_LONG
                ).show()
                viewModel.limpiarEstado()
            }
            else -> {}
        }
    }

    private fun habilitarBotones(habilitar: Boolean) {
        with(binding) {
            btnApagar.isEnabled = habilitar
            btnReiniciarEquipo.isEnabled = habilitar
            btnIniciarSteam.isEnabled = habilitar
            btnCerrarSteam.isEnabled = habilitar
            btnReiniciarSteam.isEnabled = habilitar
            btnIniciarModoJuegos.isEnabled = habilitar
            btnDetenerModoJuegos.isEnabled = habilitar
            btnCerrarRetroarch.isEnabled = habilitar
            btnEnfocarJuego.isEnabled = habilitar
            btnDetenerJuego.isEnabled = habilitar
            btnReconectarMando.isEnabled = habilitar
            btnSubirVolumen.isEnabled = habilitar
            btnBajarVolumen.isEnabled = habilitar
            btnSilenciar.isEnabled = habilitar
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
