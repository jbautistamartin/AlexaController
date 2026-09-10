// AlexaController - Sistema de automatización de PC por voz mediante Alexa.
// Copyright (C) 2026  José Luis Bautista Martín
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

using System.Drawing.Drawing2D;
using Microsoft.Win32;

namespace AlexaController.UI
{
    /// <summary>
    /// Ventana sin bordes que informa del avance del modo juegos: paso actual, barra de
    /// progreso y las últimas líneas del registro. Vive en su propio hilo STA
    /// (<see cref="Helpers.ProgresoHelper"/>), así que todos sus métodos públicos han de
    /// invocarse desde ese hilo.
    /// </summary>
    internal sealed class VentanaProgreso : Form
    {
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int MaxDetalles = 5;
        private const int MaxCaracteresDetalle = 110;

        private static readonly Color ColorFondo = Color.FromArgb(17, 20, 24);
        private static readonly Color ColorBorde = Color.FromArgb(48, 54, 62);
        private static readonly Color ColorTitulo = Color.FromArgb(236, 240, 244);
        private static readonly Color ColorAcento = Color.FromArgb(110, 168, 254);
        private static readonly Color ColorHecho = Color.FromArgb(126, 214, 143);
        private static readonly Color ColorTenue = Color.FromArgb(126, 134, 146);

        private readonly Label _titulo = new();
        private readonly Label _paso = new();
        private readonly Label _contador = new();
        private readonly Label _detalle = new();
        private readonly BarraProgreso _barra = new();
        private readonly Queue<string> _detalles = new();
        private readonly int _totalPasos;
        private int _pasoActual;

        public VentanaProgreso(string titulo, int totalPasos)
        {
            _totalPasos = Math.Max(totalPasos, 1);

            SuspendLayout();
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = ColorFondo;
            ClientSize = new Size(660, 276);
            Text = titulo;

            _titulo.Text = titulo;
            _titulo.Font = new Font("Segoe UI", 17F, FontStyle.Bold, GraphicsUnit.Point);
            _titulo.ForeColor = ColorTitulo;
            _titulo.AutoSize = true;
            _titulo.Location = new Point(26, 22);

            _paso.Text = "Preparando...";
            _paso.Font = new Font("Segoe UI", 11.5F, FontStyle.Regular, GraphicsUnit.Point);
            _paso.ForeColor = ColorAcento;
            _paso.AutoEllipsis = true;
            _paso.UseMnemonic = false;
            _paso.Bounds = new Rectangle(26, 70, 608, 26);

            _barra.Bounds = new Rectangle(26, 106, 608, 8);

            _contador.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
            _contador.ForeColor = ColorTenue;
            _contador.TextAlign = ContentAlignment.MiddleRight;
            _contador.Bounds = new Rectangle(26, 120, 608, 20);

            _detalle.Font = new Font("Segoe UI", 8.75F, FontStyle.Regular, GraphicsUnit.Point);
            _detalle.ForeColor = ColorTenue;
            _detalle.UseMnemonic = false;
            _detalle.Bounds = new Rectangle(26, 158, 608, 96);

            Controls.AddRange(new Control[] { _titulo, _paso, _barra, _contador, _detalle });
            ResumeLayout(false);

            Centrar();
            SystemEvents.DisplaySettingsChanged += AlCambiarPantallas;
        }

        /// <summary>Nunca roba el foco: es informativa y no debe estorbar a lo que se arranca.</summary>
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var parametros = base.CreateParams;
                // TOOLWINDOW la saca del Alt+Tab (y de la lista de ventanas a cerrar);
                // NOACTIVATE evita que se lleve el foco al mostrarse.
                parametros.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return parametros;
            }
        }

        public void EstablecerPaso(string texto)
        {
            _pasoActual = Math.Min(_pasoActual + 1, _totalPasos);
            _paso.ForeColor = ColorAcento;
            _paso.Text = texto;
            _contador.Text = $"Paso {_pasoActual} de {_totalPasos}";

            // El divisor lleva un paso de más para que la barra avance ya en el primero
            // y no llegue al 100 % hasta que todo haya terminado de verdad.
            _barra.Establecer((float)_pasoActual / (_totalPasos + 1));
        }

        public void AnadirDetalle(string texto)
        {
            var linea = texto.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (linea.Length == 0) return;

            if (linea.Length > MaxCaracteresDetalle)
                linea = string.Concat(linea.AsSpan(0, MaxCaracteresDetalle - 1), "…");

            _detalles.Enqueue(linea);
            while (_detalles.Count > MaxDetalles) _detalles.Dequeue();

            _detalle.Text = string.Join(Environment.NewLine, _detalles);
        }

        public void Completar(string texto)
        {
            _paso.ForeColor = ColorHecho;
            _paso.Text = texto;
            _contador.Text = string.Empty;
            _barra.Establecer(1f);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using var lapiz = new Pen(ColorBorde);
            e.Graphics.DrawRectangle(lapiz, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            e.Graphics.DrawLine(lapiz, 26, 146, ClientSize.Width - 26, 146);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= AlCambiarPantallas;
            base.OnFormClosed(e);
        }

        // El modo juegos cambia la topología de monitores con la ventana ya abierta; sin esto
        // se quedaría en las coordenadas del escritorio anterior (o fuera de pantalla).
        private void AlCambiarPantallas(object? sender, EventArgs e)
        {
            if (!IsHandleCreated || IsDisposed) return;
            BeginInvoke(Centrar);
        }

        private void Centrar()
        {
            var pantalla = (Screen.PrimaryScreen ?? Screen.AllScreens[0]).WorkingArea;
            Location = new Point(
                pantalla.X + (pantalla.Width - Width) / 2,
                pantalla.Y + (pantalla.Height - Height) / 2);
        }

        /// <summary>
        /// Barra de progreso propia: la de Windows no admite colores en modo oscuro. Avanza
        /// suavemente hacia el objetivo y recorre un brillo que deja claro que sigue trabajando.
        /// </summary>
        private sealed class BarraProgreso : Control
        {
            private static readonly Color ColorPista = Color.FromArgb(38, 43, 50);
            private const float AnchoBrillo = 130f;

            private readonly System.Windows.Forms.Timer _animacion;
            private float _objetivo;
            private float _actual;
            private float _brillo;

            public BarraProgreso()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

                _animacion = new System.Windows.Forms.Timer { Interval = 33 };
                _animacion.Tick += (_, _) =>
                {
                    _actual += (_objetivo - _actual) * 0.12f;
                    _brillo += 0.012f;
                    if (_brillo > 1f) _brillo -= 1f;
                    Invalidate();
                };
                _animacion.Start();
            }

            public void Establecer(float valor) => _objetivo = Math.Clamp(valor, 0f, 1f);

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (var pista = new SolidBrush(ColorPista))
                using (var caminoPista = Redondeado(new RectangleF(0, 0, Width, Height)))
                    g.FillPath(pista, caminoPista);

                var ancho = Width * _actual;
                if (ancho < 1f) return;

                using var camino = Redondeado(new RectangleF(0, 0, ancho, Height));
                using (var relleno = new SolidBrush(ColorAcento))
                    g.FillPath(relleno, camino);

                // Brillo que recorre la parte ya completada.
                var zona = new RectangleF((ancho + AnchoBrillo) * _brillo - AnchoBrillo, 0, AnchoBrillo, Height);

                using var brocha = new LinearGradientBrush(zona, Color.Transparent, Color.Transparent, LinearGradientMode.Horizontal)
                {
                    InterpolationColors = new ColorBlend
                    {
                        Colors = new[] { Color.FromArgb(0, 255, 255, 255), Color.FromArgb(90, 255, 255, 255), Color.FromArgb(0, 255, 255, 255) },
                        Positions = new[] { 0f, 0.5f, 1f }
                    }
                };

                var estado = g.Save();
                g.SetClip(camino);
                g.FillRectangle(brocha, zona);
                g.Restore(estado);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _animacion.Dispose();
                base.Dispose(disposing);
            }

            private static GraphicsPath Redondeado(RectangleF area)
            {
                var camino = new GraphicsPath();
                var radio = Math.Min(area.Height, area.Width) / 2f;
                if (radio <= 0.5f)
                {
                    camino.AddRectangle(area);
                    return camino;
                }

                var diametro = radio * 2f;
                camino.AddArc(area.X, area.Y, diametro, diametro, 90, 180);
                camino.AddArc(area.Right - diametro, area.Y, diametro, diametro, 270, 180);
                camino.CloseFigure();
                return camino;
            }
        }
    }
}
