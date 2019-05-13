using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Threading;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Dsp;

using System.Diagnostics;

namespace Entrada
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WaveIn waveIn;
        DispatcherTimer timer;
        Stopwatch cronometro;
        string letraAnterior = "";
        string letraActual = "";


        float frecuanciaFundamental = 0.0f;


        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            cronometro = new Stopwatch();
            LlenarComboDispositivos();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (frecuanciaFundamental >= 500)
            {
                var leftCarro = Canvas.GetLeft(imgCarro);
                Canvas.SetLeft(imgCarro, leftCarro + (frecuanciaFundamental / 500.0) * 1.2);

            }
            else
            {
                Canvas.SetLeft(imgCarro, 10);

            }

            //Texto

            if (letraActual != "" && letraActual == letraAnterior)
            {
                //Evaluar
                if (cronometro.ElapsedMilliseconds >= 100)
                {
                    txtTexto.AppendText(letraActual);
                    letraActual = "";
                    cronometro.Restart();

                    if (txtTexto.Text.Length >= 2)
                    {
                        txtTexto.Text.Substring(txtTexto.Text.Length - 2, 2);

                        if (texto == "EO")
                        {
                            lblEO.Visibility =
                                Visibility.Visible;


                        }


                    }

                }
            }
            else
            {
                cronometro.Restart();

            }
        }

        public void LlenarComboDispositivos()
        {
            for(int i= 0; i<WaveIn.DeviceCount; i++)
            {
                WaveInCapabilities capacidades = WaveIn.GetCapabilities(i);
                cbDispositivo.Items.Add(capacidades.ProductName);
            }
            cbDispositivo.SelectedIndex = 0;
        }

        private void btnIniciar_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            waveIn = new WaveIn();
            //Formato de Audio
            waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
            //Buffer
            waveIn.BufferMilliseconds = 250;

            //¿Que hacer cuando hay muestras disponibles?
            waveIn.DataAvailable += WaveIn_DataAvailable;

            //Comienza a obtener muestras
            waveIn.StartRecording();

        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] buffer = e.Buffer;
            int bytesGrabados = e.BytesRecorded;
            float acumulador = 0.0f;

            double numeroDeMuestras = bytesGrabados / 2.0f;
            int exponente = 1;
            int numeroDeMuestrasComplejas = 0;
            int bitsMaximos = 0;

            do
            {
                bitsMaximos = (int)Math.Pow(2, exponente);
                exponente++;
            } while (bitsMaximos < numeroDeMuestras);

            numeroDeMuestrasComplejas = bitsMaximos / 2;
            exponente-=2;

            Complex[] señalComleja = new Complex[numeroDeMuestrasComplejas];


            for (int i=0; i<bytesGrabados; i += 2)
            {
                //Transformando 2  bytes separados en una muestra de 16 bits 
                //1.- Toma el segundo byte y le antepone 8 0's al principio.
                //2.- Hace un OR con el primer byte, al cual automaticamente se llenan 8 0's al final.
                short muestra = (short)(buffer[i + 1] << 8 | buffer[i]);

                float muestra32bits = (float)muestra / 327668.0f;
                acumulador += Math.Abs(muestra32bits);

                if(i/2 < numeroDeMuestrasComplejas)
                {
                    señalComleja[i / 2].X = muestra32bits;
                }

            }
            float promedio = acumulador / (bytesGrabados / 2.0f);
            sldMicrofono.Value = (double)promedio;

            //FastFourierTransform.FFT()

            if(promedio > 0)
            {
                FastFourierTransform.FFT(true, exponente, señalComleja);

                float[] valoresAbsolutos =
                    new float[señalComleja.Length];
                for (int i=0; i < señalComleja.Length; i++)
                {
                    valoresAbsolutos[i] = (float)Math.Sqrt(
                        (señalComleja[i].X * señalComleja[i].X) +
                        (señalComleja[i].Y * señalComleja[i].Y)); 

                }

                int indiceSeñalConMasPresencia =
                    valoresAbsolutos.ToList().IndexOf(valoresAbsolutos.Max());

                frecuanciaFundamental =
                    (float)(indiceSeñalConMasPresencia * waveIn.WaveFormat.SampleRate) /
                    (float)valoresAbsolutos.Length;

                letraAnterior = letraActual;
                if (frecuanciaFundamental >= 500 && frecuanciaFundamental <= 550)
                {

                    letraActual = "A";

                }

                else if (frecuanciaFundamental >= 600 && frecuanciaFundamental <= 650)
                {
                    letraActual = "E";

                }

                lbl_Frecuencia.Text = frecuanciaFundamental.ToString("f");
                



            }

        }

        private void btnDetener_Click(object sender, RoutedEventArgs e)
        {
            waveIn.StopRecording();
        }
    }
}
