﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO.IsolatedStorage;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DJStudio.Models;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DJStudio
{
    public enum DialModeEnum
    {
        None,
        Color,
        Tempo,
        Volume
    };


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Dashboard : Page
    {
        public async void LoadAnotherWindow()
        {

            var viewId = 0;

            var newView = CoreApplication.CreateNewView();
            await newView.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    var frame = new Frame();
                    frame.Navigate(typeof(Display), null);
                    Window.Current.Content = frame;

                    viewId = ApplicationView.GetForCurrentView().Id;



                    Window.Current.Activate();
                });

            var viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(viewId);



        }

        RadialController myController;
        private Color[] colours = {Colors.White, Colors.Red, Colors.Aqua, Colors.Blue, Colors.BlueViolet, Colors.GreenYellow, Colors.DeepPink, Colors.DeepSkyBlue, Colors.SpringGreen, Colors.Azure, Colors.MediumSlateBlue, Colors.Crimson, Colors.DarkOrange, Colors.Aquamarine, Colors.Gold, Colors.Fuchsia};
        public int cIndex = 0;

        public string CurrentSoundKey { get; set; }
        public AudioPlayer Songplayer = new AudioPlayer();
        public AudioPlayer DjEffectplayer = new AudioPlayer();
        public DialModeEnum DialMode = DialModeEnum.None;


        DispatcherTimer dispatcherTimer = new DispatcherTimer();




        public Dashboard()
        {
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
            dispatcherTimer.Start();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            this.InitializeComponent();

            LoadAnotherWindow();

            // Create a reference to the RadialController.
            myController = RadialController.CreateForCurrentView();
            var config = RadialControllerConfiguration.GetForCurrentView();
            // config.SetDefaultMenuItems(new List<RadialControllerSystemMenuItemKind>());

            //  RadialControllerConfiguration myConfiguration =RadialControllerConfiguration.GetForCurrentView();
            config.SetDefaultMenuItems(new[]
            {
    RadialControllerSystemMenuItemKind.Volume, RadialControllerSystemMenuItemKind.NextPreviousTrack
  });


            // Create an icon for the custom tool.
            var colourIcon =
                RandomAccessStreamReference.CreateFromUri(
                    new Uri("http://orig15.deviantart.net/c880/f/2015/155/8/e/rainbow_explosion__transparent__by_thatbluecreeper-d8w2c7r.png"));
            var tempoIcon =
                RandomAccessStreamReference.CreateFromUri(
                    new Uri("https://cdn1.iconfinder.com/data/icons/agile/500/agile_velocity-512.png"));
            // Create a menu item for the custom tool.
            var Tempo =
              RadialControllerMenuItem.CreateFromIcon("Tempo", tempoIcon);
            var Colour =
              RadialControllerMenuItem.CreateFromIcon("Colour", colourIcon);


            // Add the custom tool to the RadialController menu.


            myController.Menu.Items.Add(Tempo);
            myController.Menu.Items.Add(Colour);

            // Declare input handlers for the RadialController.
            myController.ButtonClicked += MyController_ButtonClicked;
            myController.RotationChanged += MyController_RotationChanged;
            myController.ControlAcquired += MyController_ControlAcquired;
            myController.RotationResolutionInDegrees = 10;
            myController.UseAutomaticHapticFeedback = true;

        }

        private async void DispatcherTimer_Tick(object sender, object e)
        {
            var n = Songplayer.GetAudioFileInputNode(CurrentSoundKey);
          


            if (n != null)
            {
                var d = new Dictionary<int, double>();
                var x = n.Position.TotalSeconds;

                var audioFrame = Songplayer.FrameOutput.GetFrame();


                using (var lockedBuffer = audioFrame.LockBuffer(Windows.Media.AudioBufferAccessMode.ReadWrite))
                {
                    using (var refference = lockedBuffer.CreateReference())
                    {
                        await Task.Run(() =>
                        {
                            unsafe
                            {
                                var memoryByteAccess = refference as IMemoryBufferByteAccess;
                                var chanels = (int)Songplayer.FrameOutput.EncodingProperties.ChannelCount;
                                var sampleRate = (int)Songplayer.FrameOutput.EncodingProperties.SampleRate;

                                byte* p;
                                uint capacity;
                                memoryByteAccess.GetBuffer(out p, out capacity);

                                int length = (int)(capacity / sizeof(Int16));
                                var seconds = length / (double)Songplayer.FrameOutput.EncodingProperties.SampleRate / chanels;


                                Int16* b = (Int16*)(p);
                                var erg = new short[length];
                                for (int i = 0; i < erg.Length; i++)
                                {
                                    erg[i] = b[i];


                                }
                                for (int i = 0; i < erg.Length - 1; i = i + 2)
                                {
                                    var b1 = erg[i];
                                    var b2 = erg[i + 1];
                                    //get amplitude
                                    var amplitude = (double)(b2 << 8 | b1 & 0xFF) / 32767.0;
                                    if (Math.Abs(amplitude) != 0)
                                    {
                                        d.Add(i, amplitude);

                                    }



                                }



                            }

                        });
                    }
                }

                //foreach (var item in d.Take(10))
                //{
                //    var line = new Line();
                //    var rnd = new Random();
                //    var strokeThickness = rnd.Next(1, 3);

                //    line.StrokeThickness = strokeThickness;


                //    //line.Width = 50;
                //    line.X1 = item.Key;
                //    line.Y1 = 0;
                //    line.X2 = item.Key;
                //    line.Y2 = item.Value;

                //    //Canvas.SetTop(line, item.Value);
                //    //Canvas.SetLeft(line, item.Value);

                //    var ellipse1 = new Ellipse();
                //    ellipse1.Fill = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
                //    ellipse1.Width = strokeThickness*item.Key;
                //    ellipse1.Height = Math.Abs( strokeThickness*item.Value);

                //    TheCanvas.Children.Add(line);
                //    TheCanvas.Children.Add(ellipse1);
                //}

                //var ellipse1 = new Ellipse();
                //Random randomGen = new Random();
                //var value1 = randomGen.Next(1, 10);
                //var value2 = randomGen.Next(1, 10);

                //Random rnd = new Random();
                //Windows.UI.Color clr = Windows.UI.Color.FromArgb((byte)rnd.Next(255), (byte)rnd.Next(255), (byte)rnd.Next(255), (byte)rnd.Next(255));
                //ellipse1.Fill = new SolidColorBrush(clr);
                //ellipse1.Width = (int)(value1 * 10/ value2);
                //ellipse1.Height = (int)(value1 * 20/ value2);

                //HeaderPanel.Children.Add(ellipse1);
            }

        }

        private void MyController_ControlAcquired(RadialController sender, RadialControllerControlAcquiredEventArgs args)
        {

            var s = sender.Menu.GetSelectedMenuItem();
            if (sender.Menu.GetSelectedMenuItem().DisplayText == "Colour")
            {
                this.DialMode = DialModeEnum.Color;
            }
            else if (sender.Menu.GetSelectedMenuItem().DisplayText == "Tempo")
            {
                this.DialMode = DialModeEnum.Tempo;
            }
            else if (sender.Menu.GetSelectedMenuItem().DisplayText == "Volume")
            {
                this.DialMode = DialModeEnum.Volume;
            }
            else
            {
                this.DialMode = DialModeEnum.None;
            }
        }

        // Handler for rotation input from the RadialController.
        private void MyController_RotationChanged(RadialController sender,
          RadialControllerRotationChangedEventArgs args)
        {
            switch (this.DialMode)
            {
                case DialModeEnum.Color:
                    Colour_RotationChanged(args);
                    break;
                case DialModeEnum.Tempo:
                    Tempo_RotationChanged(args);
                    break;
                case DialModeEnum.Volume:
                    Volume_RotationChanged(args);
                    break;
               
            }
            Volume_RotationChanged(args);
        }

        private void Tempo_RotationChanged(RadialControllerRotationChangedEventArgs args)
        {
            if ((playSpeedSlider.Value <= 1.5) && (playSpeedSlider.Value >= 0.75))
            {
                if (args.RotationDeltaInDegrees > 0)
                {
                    playSpeedSlider.Value += 0.1;
                    return;
                }
                else if (args.RotationDeltaInDegrees < 0)
                {
                    playSpeedSlider.Value -= 0.1;
                    return;

                }
            }
        }

        private void Colour_RotationChanged(RadialControllerRotationChangedEventArgs args)
        {
            //lerp?
            
            if (args.RotationDeltaInDegrees > 0)
            {
                if(cIndex < colours.Length-1)
                cIndex++;
            }
            if (args.RotationDeltaInDegrees < 0)
            {
                if(cIndex >= 1)
                    cIndex--;
            }
            EllipseLeft.Fill = new SolidColorBrush(colours[cIndex]);
            EllipseRight.Fill = EllipseLeft.Fill;
        }

        private void Volume_RotationChanged(RadialControllerRotationChangedEventArgs args)
        {
           /* if (RotationSlider.Value + args.RotationDeltaInDegrees > 100)
            {
                RotationSlider.Value = 100;
                return;
            }
            else if (RotationSlider.Value + args.RotationDeltaInDegrees < 0)
            {
                RotationSlider.Value = 0;
                return;
            }

            RotationSlider.Value += args.RotationDeltaInDegrees;*/
        }

        // Handler for click input from the RadialController.
        private void MyController_ButtonClicked(RadialController sender,
          RadialControllerButtonClickedEventArgs args)
        {
            ButtonToggle.IsOn = !ButtonToggle.IsOn;
        }
        private void PlaySpeedSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Songplayer.PlaybackSpeedFactor(CurrentSoundKey, playSpeedSlider.Value);
        }


        private async void songslistView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (songslistView.SelectedIndex >= 0)
            {
                var currentSong = songslistView.Items[songslistView.SelectedIndex] as Windows.UI.Xaml.Controls.ListViewItem;
                if (currentSong != null)
                {

                    Songplayer.Stop(CurrentSoundKey);
                    var path = $"ms-appx:///Assets/Audio/{currentSong.Content}.mp3";
                    var wav = await StorageFile.GetFileFromApplicationUriAsync(new Uri(path));

                    await Songplayer.LoadFileAsync(wav);

                    Songplayer.Play($"{currentSong.Content}.mp3", 0.5);
                    CurrentSoundKey = $"{currentSong.Content}.mp3";
                }

            }


        }

        private async void Effect_Click(object sender, RoutedEventArgs e)
        {
            var currentEffect = sender as Button;
            if (currentEffect != null)
            {
                var path = $"ms-appx:///Assets/Audio/Effects/{currentEffect.Content}.mp3";
                var wav = await StorageFile.GetFileFromApplicationUriAsync(new Uri(path));

                await DjEffectplayer.LoadFileAsync(wav);

                DjEffectplayer.Play($"{currentEffect.Content}.mp3", 0.75);



            }
        }
        // Change effect paramters to reflect UI control
        private void Eq1Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Songplayer.EqEffectDefinition != null)
            {
                double currentValue = ConvertRange(eq1Slider.Value);
                Songplayer.EqEffectDefinition.Bands[0].Gain = currentValue;
            }
        }

        private void Eq2Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Songplayer.EqEffectDefinition != null)
            {
                double currentValue = ConvertRange(eq2Slider.Value);
                Songplayer.EqEffectDefinition.Bands[1].Gain = currentValue;
            }
        }

        private void Eq3Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Songplayer.EqEffectDefinition != null)
            {
                double currentValue = ConvertRange(eq3Slider.Value);
                Songplayer.EqEffectDefinition.Bands[2].Gain = currentValue;
            }
        }

        private void Eq4Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Songplayer.EqEffectDefinition != null)
            {
                double currentValue = ConvertRange(eq4Slider.Value);
                Songplayer.EqEffectDefinition.Bands[3].Gain = currentValue;
            }
        }

        // Mapping the 0-100 scale of the slider to a value between the min and max gain
        private double ConvertRange(double value)
        {
            // These are the same values as the ones in xapofx.h
            const double fxeq_min_gain = 0.126;
            const double fxeq_max_gain = 7.94;

            double scale = (fxeq_max_gain - fxeq_min_gain) / 100;
            return (fxeq_min_gain + ((value) * scale));
        }

        //public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        //{
        //   // if (buffer == null) throw new ArgumentNullException("buffer");

        //    Func<CancellationToken, IProgress<uint>, Task<IBuffer>> taskProvider =
        //    (token, progress) => ReadBytesAsync(buffer, count, token, progress, options);

        //    return AsyncInfo.Run(taskProvider);
        //}

        //private async Task<IBuffer> ReadBytesAsync(IBuffer buffer, uint count, CancellationToken token, IProgress<uint> progress, InputStreamOptions options)
        //{

        //    return buffer;
        //}

       

        private void ColourController_SelectionChanged(object sender, RoutedEventArgs e)
        {
                    }
    }
}
