package com.capicua.gamecontroller.presentation.settings

import android.net.Uri
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.activity.result.contract.ActivityResultContracts
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.capicua.gamecontroller.R
import com.capicua.gamecontroller.data.config.AppConfig
import com.capicua.gamecontroller.databinding.FragmentSettingsBinding
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.google.android.material.snackbar.Snackbar
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.launch
import java.time.LocalDate
import java.time.format.DateTimeFormatter

@AndroidEntryPoint
class SettingsFragment : Fragment() {

    private var _binding: FragmentSettingsBinding? = null
    private val binding get() = _binding!!
    private val viewModel: SettingsViewModel by viewModels()

    private var configCargada = false

    // Storage Access Framework: el usuario elige el archivo, así que no hace falta ningún permiso.
    private val crearArchivo = registerForActivityResult(
        ActivityResultContracts.CreateDocument("application/json")
    ) { uri: Uri? ->
        if (uri != null && _binding != null) viewModel.exportar(uri, leerConfig())
    }

    private val abrirArchivo = registerForActivityResult(
        ActivityResultContracts.OpenDocument()
    ) { uri: Uri? ->
        if (uri != null) viewModel.importar(uri)
    }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentSettingsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        observarConfig()
        observarEstado()
        configurarBotones()
    }

    private fun observarConfig() {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                viewModel.config.collect { config ->
                    if (!configCargada) {
                        rellenarFormulario(config)
                        configCargada = true
                    }
                }
            }
        }
    }

    private fun rellenarFormulario(config: AppConfig) {
        binding.editUrl.setText(config.urlServidor)
        binding.editUsuario.setText(config.usuario)
        binding.editContrasena.setText(config.contrasena)
        binding.switchCertInvalidos.isChecked = config.aceptarCertificadosInvalidos
        binding.editIntervaloLog.setText(config.intervaloRefrescoLog.toString())
        binding.switchIrAlLog.isChecked = config.irAlLogAlEjecutar
    }

    private fun observarEstado() {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                viewModel.estado.collect { estado ->
                    actualizarEstado(estado)
                }
            }
        }
    }

    private fun actualizarEstado(estado: SettingsUiState) {
        val probando = estado is SettingsUiState.Probando
        binding.btnProbar.isEnabled = !probando
        binding.btnGuardar.isEnabled = !probando
        binding.progressProbar.visibility = if (probando) View.VISIBLE else View.GONE

        when (estado) {
            is SettingsUiState.Guardado -> {
                Snackbar.make(binding.root, R.string.settings_guardado, Snackbar.LENGTH_SHORT).show()
                viewModel.limpiarEstado()
            }
            is SettingsUiState.ConexionOk -> {
                Snackbar.make(binding.root, R.string.settings_conexion_ok, Snackbar.LENGTH_SHORT).show()
                viewModel.limpiarEstado()
            }
            is SettingsUiState.ConexionError -> {
                Snackbar.make(binding.root, estado.mensaje, Snackbar.LENGTH_LONG).show()
                viewModel.limpiarEstado()
            }
            is SettingsUiState.Exportado -> {
                Snackbar.make(binding.root, R.string.settings_exportado, Snackbar.LENGTH_SHORT).show()
                viewModel.limpiarEstado()
            }
            is SettingsUiState.Importado -> {
                rellenarFormulario(estado.config)
                Snackbar.make(binding.root, R.string.settings_importado, Snackbar.LENGTH_SHORT).show()
                viewModel.limpiarEstado()
            }
            is SettingsUiState.ErrorArchivo -> {
                Snackbar.make(
                    binding.root,
                    getString(R.string.settings_error_archivo, estado.mensaje),
                    Snackbar.LENGTH_LONG
                ).show()
                viewModel.limpiarEstado()
            }
            else -> {}
        }
    }

    private fun configurarBotones() {
        binding.btnGuardar.setOnClickListener { viewModel.guardar(leerConfig()) }
        binding.btnProbar.setOnClickListener { viewModel.probarConexion(leerConfig()) }
        binding.btnExportar.setOnClickListener {
            crearArchivo.launch(getString(R.string.settings_backup_archivo, fechaArchivo()))
        }
        binding.btnImportar.setOnClickListener {
            MaterialAlertDialogBuilder(requireContext())
                .setTitle(R.string.confirmar_titulo)
                .setMessage(R.string.settings_importar_confirmar)
                .setPositiveButton(R.string.confirmar_si) { _, _ ->
                    // Algunos exploradores no reconocen el tipo de los .json compartidos.
                    abrirArchivo.launch(
                        arrayOf("application/json", "text/plain", "application/octet-stream")
                    )
                }
                .setNegativeButton(R.string.confirmar_no, null)
                .show()
        }
    }

    private fun fechaArchivo(): String =
        LocalDate.now().format(DateTimeFormatter.ofPattern("yyyyMMdd"))

    private fun leerConfig(): AppConfig = AppConfig(
        urlServidor = binding.editUrl.text.toString().trim(),
        usuario = binding.editUsuario.text.toString().trim(),
        contrasena = binding.editContrasena.text.toString(),
        aceptarCertificadosInvalidos = binding.switchCertInvalidos.isChecked,
        intervaloRefrescoLog = binding.editIntervaloLog.text.toString().toIntOrNull() ?: 5,
        irAlLogAlEjecutar = binding.switchIrAlLog.isChecked
    )

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
