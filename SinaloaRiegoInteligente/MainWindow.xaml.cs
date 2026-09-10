using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SinaloaRiegoInteligente
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Parcela> Parcelas { get; set; }
        private DispatcherTimer _timer;
        private Random _random = new Random();

        public MainWindow()
        {
            InitializeComponent();

            // Sectores agrícolas de prueba
            Parcelas = new ObservableCollection<Parcela>
            {
                new Parcela { Nombre = "Sector 01 - Valle Culiacán", Cultivo = "Maíz", HumedadSuelo = 32.0, Temperatura = 29.5, Hectareas = 5.0 },
                new Parcela { Nombre = "Sector 02 - Carrizo", Cultivo = "Frijol", HumedadSuelo = 58.5, Temperatura = 26.0, Hectareas = 3.5 },
                new Parcela { Nombre = "Sector 03 - Guasave", Cultivo = "Jitomate", HumedadSuelo = 28.0, Temperatura = 31.2, Hectareas = 2.0 },
                new Parcela { Nombre = "Sector 04 - Guamúchil", Cultivo = "Garbanzo", HumedadSuelo = 30.5, Temperatura = 38.0, Hectareas = 30.0 }
            };

            DataContext = this;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Tick += SimularSensores;
            _timer.Start();
        }

        private void SimularSensores(object? sender, EventArgs e)
        {
            foreach (var p in Parcelas)
            {
                p.HumedadSuelo += (_random.NextDouble() * 2) - 1;
                p.HumedadSuelo = Math.Clamp(Math.Round(p.HumedadSuelo, 1), 10, 90);
            }
            ListaParcelas.Items.Refresh();
        }

        private void BtnActivarRiego_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Parcela parcela)
            {
                double litrosRequeridos = parcela.CalcularLitrosAguaNecesarios();

                if (litrosRequeridos > 0)
                {
                    MessageBoxResult respuesta = MessageBox.Show(
                        $"Análisis Hídrico Inteligente:\n\n" +
                        $"• Sector: {parcela.Nombre}\n" +
                        $"• Cultivo: {parcela.Cultivo} ({parcela.Hectareas} ha)\n" +
                        $"• Humedad Actual: {parcela.HumedadSuelo}%\n" +
                        $"• Volumen de Riego Sugerido: {litrosRequeridos:N0} Litros\n\n" +
                        $"¿Desea enviar una alerta de notificación por WhatsApp al agricultor?",
                        "Recomendación de Riego Optimizada",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (respuesta == MessageBoxResult.Yes)
                    {
                        // NÚMERO CONFIGURADO: LADA 52 + 6731420576
                        string numeroWhatsApp = "526731420576";
                        EnviarAlertaWhatsApp(numeroWhatsApp, parcela.Nombre, parcela.Cultivo, parcela.HumedadSuelo, litrosRequeridos);
                    }
                }
                else
                {
                    MessageBox.Show(
                        $"El {parcela.Nombre} cuenta con un nivel hídrico óptimo ({parcela.HumedadSuelo}%).\nNo requiere riego adicional en este momento.",
                        "Eficiencia Hídrica",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// Método que genera y abre el enlace de WhatsApp
        /// </summary>
        private void EnviarAlertaWhatsApp(string numeroTelefono, string sector, string cultivo, double humedad, double litros)
        {
            string mensaje = $"*ALERTA DE RIEGO - SINALOA IOT*\n\n" +
                             $"⚠️ *Sector:* {sector}\n" +
                             $"🌱 *Cultivo:* {cultivo}\n" +
                             $"💧 *Humedad Actual:* {humedad}%\n" +
                             $"📊 *Estado:* Estrés Hídrico Detectado\n\n" +
                             $"💧 *Volumen Sugerido:* {litros:N0} Litros.\n\n" +
                             $"_Responda '1' para autorizar la apertura automatizada de válvulas._";

            string mensajeCodificado = WebUtility.UrlEncode(mensaje);
            string urlWhatsApp = $"https://wa.me/{numeroTelefono}?text={mensajeCodificado}";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = urlWhatsApp,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir el navegador: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Muestra todas las parcelas nuevamente (Vista Dashboard)
        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            ListaParcelas.ItemsSource = Parcelas;
        }

        // Filtra la lista para mostrar SOLO las parcelas en Estrés Hídrico (Menos de 35% humedad)
        private void BtnAlertas_Click(object sender, RoutedEventArgs e)
        {
            var soloAlertas = new List<Parcela>();
            foreach (var p in Parcelas)
            {
                if (p.HumedadSuelo < 35.0)
                {
                    soloAlertas.Add(p);
                }
            }

            ListaParcelas.ItemsSource = soloAlertas;

            if (soloAlertas.Count == 0)
            {
                MessageBox.Show("No hay parcelas en estado crítico en este momento.", "Alertas de Riego", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Genera un resumen global acumulado y exporta un archivo de reporte
        private void BtnReporte_Click(object sender, RoutedEventArgs e)
        {
            double totalLitrosGeneral = 0;
            string resumen = "--- REPORTE GENERAL DE EFICIENCIA HÍDRICA ---\n\n";

            foreach (var p in Parcelas)
            {
                double litros = p.CalcularLitrosAguaNecesarios();
                totalLitrosGeneral += litros;
                resumen += $"• {p.Nombre} ({p.Cultivo}): {p.HumedadSuelo}% Humedad | Requerido: {litros:N0} L\n";
            }

            resumen += $"\n----------------------------------------\nTOTAL AGUA REQUERIDA EN EL ESTADO: {totalLitrosGeneral:N0} Litros";

            MessageBoxResult exportar = MessageBox.Show(
                $"{resumen}\n\n¿Desea exportar este reporte a un archivo local de texto?",
                "Reporte Hídrico Consolidado",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (exportar == MessageBoxResult.Yes)
            {
                try
                {
                    string rutaArchivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Reporte_Hidrico_Sinaloa.txt");
                    File.WriteAllText(rutaArchivo, resumen);
                    MessageBox.Show($"Reporte guardado exitosamente en tu Escritorio:\n{rutaArchivo}", "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}