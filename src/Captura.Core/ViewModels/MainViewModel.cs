﻿using Captura.Models;
using Captura.Properties;
using Screna;
using Screna.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using Window = Screna.Window;

namespace Captura.ViewModels
{
    public partial class MainViewModel : ViewModelBase, IDisposable
    {
        #region Fields
        Timer _timer;
        Timing _timing = new Timing();
        IRecorder _recorder;
        string _currentFileName;
        bool isVideo;
        public static readonly RectangleConverter RectangleConverter = new RectangleConverter();
        readonly SynchronizationContext _syncContext = SynchronizationContext.Current;

        IWebCamProvider _cam;
        public IWebCamProvider WebCamProvider
        {
            get => _cam;
            set
            {
                _cam = value;

                OnPropertyChanged();
            }
        }
        #endregion
        
        public MainViewModel()
        {
            this.WorkViewModel = new WorkViewModel(this);

            #region Commands
            ScreenShotCommand = new DelegateCommand(() => CaptureScreenShot());
            
            ScreenShotActiveCommand = new DelegateCommand(() => SaveScreenShot(ScreenShotWindow(Window.ForegroundWindow)));

            ScreenShotDesktopCommand = new DelegateCommand(() => SaveScreenShot(ScreenShotWindow(Window.DesktopWindow)));

            RecordCommand = new DelegateCommand(async () =>
            {
                if (RecorderState == RecorderState.NotRecording)
                    StartRecording();
                else await StopRecording();
            });

            RefreshCommand = new DelegateCommand(() =>
            {
                VideoViewModel.RefreshVideoSources();

                VideoViewModel.RefreshCodecs();

                AudioViewModel.AudioSource.Refresh();

                Status.LocalizationKey = nameof(Resources.Refreshed);
            });
            
            PauseCommand = new DelegateCommand(() =>
            {
                if (RecorderState == RecorderState.Paused)
                {
                    ServiceProvider.SystemTray.HideNotification();

                    _recorder.Start();
                    _timing?.Start();
                    _timer?.Start();
                    
                    RecorderState = RecorderState.Recording;
                    Status.LocalizationKey = nameof(Resources.Recording);
                }
                else
                {
                    _recorder.Stop();
                    _timer?.Stop();
                    _timing?.Pause();

                    RecorderState = RecorderState.Paused;
                    Status.LocalizationKey = nameof(Resources.Paused);

                    ServiceProvider.SystemTray.ShowTextNotification(Resources.Paused, 3000, null);
                }
            }, false);
            #endregion
        }

        void RestoreRemembered()
        {
            #region Restore Video Source
            void VideoSource()
            {
                VideoViewModel.SelectedVideoSourceKind = Settings.LastSourceKind;

                var source = VideoViewModel.AvailableVideoSources.FirstOrDefault(window => window.ToString() == Settings.LastSourceName);

                if (source != null)
                    VideoViewModel.SelectedVideoSource = source;
            }

            switch (Settings.LastSourceKind)
            {
                case VideoSourceKind.Window:
                case VideoSourceKind.NoVideo:
                case VideoSourceKind.Screen:
                    VideoSource();
                    break;

                case VideoSourceKind.Region:
                    VideoViewModel.SelectedVideoSourceKind = VideoSourceKind.Region;
                    var rect = (Rectangle)RectangleConverter.ConvertFromString(Settings.LastSourceName);

                    VideoViewModel.RegionProvider.SelectedRegion = rect;
                    break;
            }
            #endregion

            // Restore Video Codec
            if (VideoViewModel.AvailableVideoWriterKinds.Contains(Settings.LastVideoWriterKind))
            {
                VideoViewModel.SelectedVideoWriterKind = Settings.LastVideoWriterKind;

                var codec = VideoViewModel.AvailableVideoWriters.FirstOrDefault(c => c.ToString() == Settings.LastVideoWriterName);

                if (codec != null)
                    VideoViewModel.SelectedVideoWriter = codec;
            }
            
            // Restore Microphone
            if (!string.IsNullOrEmpty(Settings.LastMicName))
            {
                var source = AudioViewModel.AudioSource.AvailableRecordingSources.FirstOrDefault(codec => codec.ToString() == Settings.LastMicName);

                if (source != null)
                    AudioViewModel.AudioSource.SelectedRecordingSource = source;
            }

            // Restore Loopback Speaker
            if (!string.IsNullOrEmpty(Settings.LastSpeakerName))
            {
                var source = AudioViewModel.AudioSource.AvailableLoopbackSources.FirstOrDefault(codec => codec.ToString() == Settings.LastSpeakerName);

                if (source != null)
                    AudioViewModel.AudioSource.SelectedLoopbackSource = source;
            }

            // Restore ScreenShot Format
            if (!string.IsNullOrEmpty(Settings.LastScreenShotFormat))
            {
                var format = ScreenShotImageFormats.FirstOrDefault(f => f.ToString() == Settings.LastScreenShotFormat);

                if (format != null)
                    SelectedScreenShotImageFormat = format;
            }

            // Restore ScreenShot Target
            if (!string.IsNullOrEmpty(Settings.LastScreenShotSaveTo))
            {
                var saveTo = VideoViewModel.AvailableImageWriters.FirstOrDefault(s => s.ToString() == Settings.LastScreenShotSaveTo);

                if (saveTo != null)
                    VideoViewModel.SelectedImageWriter = saveTo.Source;
            }

            // Restore Region Size
            VideoViewModel.SelectedRegionSizeKind = Settings.LastSelectedRegionSizeKind;
        }

        bool _persist, _hotkeys;

        public void Init(bool Persist, bool Timer, bool Remembered, bool Hotkeys)
        {
            _persist = Persist;
            _hotkeys = Hotkeys;

            if (Timer)
            {
                _timer = new Timer(500);
                _timer.Elapsed += TimerOnElapsed;
            }

            AudioViewModel.AudioSource.PropertyChanged += (Sender, Args) =>
            {
                switch (Args.PropertyName)
                {
                    case nameof(AudioViewModel.AudioSource.SelectedRecordingSource):
                    case nameof(AudioViewModel.AudioSource.SelectedLoopbackSource):
                    case null:
                    case "":
                        CheckFunctionalityAvailability();
                        break;
                }
            };

            VideoViewModel.PropertyChanged += (Sender, Args) =>
            {
                switch (Args.PropertyName)
                {
                    case nameof(VideoViewModel.SelectedVideoSourceKind):
                    case nameof(VideoViewModel.SelectedVideoSource):
                    case null:
                    case "":
                        CheckFunctionalityAvailability();
                        break;
                }
            };

            // If Output Dircetory is not set. Set it to Documents\Captura\
            if (string.IsNullOrWhiteSpace(Settings.OutPath))
            {
                //Settings.OutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AYoutuber\\");
                Settings.OutPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Works");
            }

            // Create the Output Directory if it does not exist
            if (!Directory.Exists(Settings.OutPath))
                Directory.CreateDirectory(Settings.OutPath);

            // Register ActionServices
            ServiceProvider.Register<Action>(ServiceName.Recording, () => RecordCommand.ExecuteIfCan());
            ServiceProvider.Register<Action>(ServiceName.Pause, () => PauseCommand.ExecuteIfCan());
            ServiceProvider.Register<Action>(ServiceName.ScreenShot, () => ScreenShotCommand.ExecuteIfCan());
            ServiceProvider.Register<Action>(ServiceName.ActiveScreenShot, () => SaveScreenShot(ScreenShotWindow(Window.ForegroundWindow)));
            ServiceProvider.Register<Action>(ServiceName.DesktopScreenShot, () => SaveScreenShot(ScreenShotWindow(Window.DesktopWindow)));
            ServiceProvider.Register<Func<Window>>(ServiceName.SelectedWindow, () => (VideoViewModel.SelectedVideoSource as WindowItem).Window);

            // Register Hotkeys if not console
            if (_hotkeys)
                HotKeyManager.RegisterAll();

            VideoViewModel.Init();

            if (Remembered)
                RestoreRemembered();

            WebCamProvider = ServiceProvider.Get<IWebCamProvider>(ServiceName.WebCam);
        }

        void Remember()
        {
            #region Remember Video Source
            switch (VideoViewModel.SelectedVideoSourceKind)
            {
                case VideoSourceKind.Window:
                case VideoSourceKind.Screen:
                case VideoSourceKind.NoVideo:
                    Settings.LastSourceKind = VideoViewModel.SelectedVideoSourceKind;
                    Settings.LastSourceName = VideoViewModel.SelectedVideoSource.ToString();
                    break;

                case VideoSourceKind.Region:
                    Settings.LastSourceKind = VideoSourceKind.Region;
                    var rect = VideoViewModel.RegionProvider.SelectedRegion;
                    Settings.LastSourceName = RectangleConverter.ConvertToString(rect);
                    Settings.LastSelectedRegionSizeKind = this.VideoViewModel.SelectedRegionSizeKind;
                    break;

                default:
                    Settings.LastSourceKind = VideoSourceKind.Screen;
                    Settings.LastSourceName = "";
                    break;
            }
            #endregion

            // Remember Video Codec
            Settings.LastVideoWriterKind = VideoViewModel.SelectedVideoWriterKind;
            Settings.LastVideoWriterName = VideoViewModel.SelectedVideoWriter.ToString();

            // Remember Audio Sources
            Settings.LastMicName = AudioViewModel.AudioSource.SelectedRecordingSource.ToString();
            Settings.LastSpeakerName = AudioViewModel.AudioSource.SelectedLoopbackSource.ToString();
            
            // Remember ScreenShot Format
            Settings.LastScreenShotFormat = SelectedScreenShotImageFormat.ToString();

            // Remember ScreenShot Target
            Settings.LastScreenShotSaveTo = VideoViewModel.SelectedImageWriter.ToString();
        }

        // Call before Exit to free Resources
        public void Dispose()
        {
            if (_hotkeys)
                HotKeyManager.Dispose();

            AudioViewModel.Dispose();

            RecentViewModel.Dispose();

            // Remember things if not console.
            if (_persist)
                Remember();
            
            // Save if not console
            if (_persist)
                Settings.Save();
        }
        
        void TimerOnElapsed(object Sender, ElapsedEventArgs Args)
        {
            TimeSpan = TimeSpan.FromSeconds((int)_timing.Elapsed.TotalSeconds);
            
            // If Capture Duration is set and reached
            if (Duration > 0 && TimeSpan.TotalSeconds >= Duration)
                _syncContext.Post(async state => await StopRecording(), null);
        }
        
        void CheckFunctionalityAvailability()
        {
            var audioAvailable = AudioViewModel.AudioSource.AudioAvailable;

            var videoAvailable = VideoViewModel.SelectedVideoSourceKind != VideoSourceKind.NoVideo;
            
            RecordCommand.RaiseCanExecuteChanged(audioAvailable || videoAvailable);

            ScreenShotCommand.RaiseCanExecuteChanged(videoAvailable);
        }

        public void SaveScreenShot(Bitmap bmp, string FileName = null)
        {
            // Save to Disk or Clipboard
            if (bmp != null)
            {
                VideoViewModel.SelectedImageWriter.Save(bmp, SelectedScreenShotImageFormat, FileName, Status, RecentViewModel);

                bmp.Dispose();
            }
            else Status.LocalizationKey = nameof(Resources.ImgEmpty);
        }

        public Bitmap ScreenShotWindow(Window hWnd)
        {
            ServiceProvider.SystemTray.HideNotification();

            if (hWnd == Window.DesktopWindow)
            {
                return ScreenShot.Capture(Settings.IncludeCursor).Transform();
            }
            else
            {
                var bmp = ScreenShot.CaptureTransparent(hWnd,
                    Settings.IncludeCursor,
                    Settings.DoResize,
                    Settings.ResizeWidth,
                    Settings.ResizeHeight);

                // Capture without Transparency
                if (bmp == null)
                {
                    return ScreenShot.Capture(hWnd, Settings.IncludeCursor)?.Transform();
                }
                else return bmp.Transform(true);
            }
        }

        public void CaptureScreenShot(string FileName = null)
        {
            ServiceProvider.SystemTray.HideNotification();

            Bitmap bmp = null;

            var selectedVideoSource = VideoViewModel.SelectedVideoSource;
            var includeCursor = Settings.IncludeCursor;

            switch (VideoViewModel.SelectedVideoSourceKind)
            {
                case VideoSourceKind.Window:
                    var hWnd = (selectedVideoSource as WindowItem)?.Window ?? Window.DesktopWindow;

                    bmp = ScreenShotWindow(hWnd);
                    break;

                case VideoSourceKind.Screen:
                    if (selectedVideoSource is FullScreenItem fullScreen)
                    {
                        bmp = ScreenShot.Capture();
                    }
                    else if (selectedVideoSource is ScreenItem screen)
                    {
                        bmp = (selectedVideoSource as ScreenItem)?.Capture(includeCursor);
                    }
                    
                    bmp = bmp?.Transform();
                    break;

                case VideoSourceKind.Region:
                    bmp = ScreenShot.Capture(VideoViewModel.RegionProvider.SelectedRegion, includeCursor);
                    bmp = bmp.Transform();
                    break;
            }

            SaveScreenShot(bmp, FileName);
        }
        
        public void StartRecording(string FileName = null)
        {
            FFMpegLog.Reset();

            VideoViewModel.RegionProvider.Lock();

            ServiceProvider.SystemTray.HideNotification();

            if (Settings.MinimizeOnStart)
                ServiceProvider.Get<Action<bool>>(ServiceName.Minimize).Invoke(true);
            
            CanChangeVideoSource = VideoViewModel.SelectedVideoSourceKind == VideoSourceKind.Window;

            Settings.Instance.EnsureOutPath();
            
            if (StartDelay < 0)
                StartDelay = 0;

            if (Duration != 0 && (StartDelay > Duration * 1000))
            {
                Status.LocalizationKey = nameof(Resources.DelayGtDuration);
                SystemSounds.Asterisk.Play();
                return;
            }

            RecorderState = RecorderState.Recording;
            
            isVideo = VideoViewModel.SelectedVideoSourceKind != VideoSourceKind.NoVideo;
            
            var extension = VideoViewModel.SelectedVideoWriter.Extension;

            if (VideoViewModel.SelectedVideoSource is NoVideoItem x)
                extension = x.Extension;

            _currentFileName = FileName ?? Path.Combine(Settings.OutPathWithWork(), DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + extension);

            Status.LocalizationKey = StartDelay > 0 ? nameof(Resources.Waiting) : nameof(Resources.Recording);

            _timer?.Stop();
            TimeSpan = TimeSpan.Zero;
            
            var audioSource = AudioViewModel.AudioSource.GetAudioSource();

            var imgProvider = GetImageProvider();
            
            var videoEncoder = GetVideoFileWriter(imgProvider, audioSource);
            
            if (_recorder == null)
            {
                if (isVideo)
                    _recorder = new Recorder(videoEncoder, imgProvider, Settings.FrameRate, audioSource);

                else if (VideoViewModel.SelectedVideoSource is NoVideoItem audioWriter)
                    _recorder = new Recorder(audioWriter.GetAudioFileWriter(_currentFileName, audioSource.WaveFormat, Settings.AudioQuality), audioSource);
            }

            _recorder.ErrorOccured += E => _syncContext.Post(d => OnErrorOccured(E), null);
            
            if (StartDelay > 0)
            {
                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(StartDelay);

                    _recorder.Start();
                });
            }
            else _recorder.Start();

            _timing?.Start();
            _timer?.Start();
        }

        void OnErrorOccured(Exception E)
        {
            Status.LocalizationKey = nameof(Resources.ErrorOccured);

            RecorderState = RecorderState.NotRecording;

            // Set Recorder to null
            _recorder = null;

            _timer?.Stop();
            _timing?.Stop();

            CanChangeVideoSource = true;

            if (Settings.MinimizeOnStart)
                ServiceProvider.Get<Action<bool>>(ServiceName.Minimize).Invoke(false);

            VideoViewModel.RegionProvider.Release();

            ServiceProvider.Messenger.ShowError($"Error Occured\n\n{E}");
        }

        IVideoFileWriter GetVideoFileWriter(IImageProvider ImgProvider, IAudioProvider AudioProvider)
        {
            if (VideoViewModel.SelectedVideoSourceKind == VideoSourceKind.NoVideo)
                return null;
            
            IVideoFileWriter videoEncoder = null;
            
            var encoder = VideoViewModel.SelectedVideoWriter.GetVideoFileWriter(_currentFileName, Settings.FrameRate, Settings.VideoQuality, ImgProvider, Settings.AudioQuality, AudioProvider);

            switch (encoder)
            {
                case GifWriter gif:
                    if (Settings.GifVariable)
                        _recorder = new VFRGifRecorder(gif, ImgProvider);
                    
                    else videoEncoder = gif;
                    break;

                default:
                    videoEncoder = encoder;
                    break;
            }

            return videoEncoder;
        }
        
        IImageProvider GetImageProvider()
        {
            Func<Point> offset = () => Point.Empty;

            var imageProvider = VideoViewModel.SelectedVideoSource?.GetImageProvider(out offset);

            if (imageProvider == null)
                return null;

            var overlays = new List<IOverlay>();

            // Mouse Click overlay should be drawn below cursor.
            if (MouseKeyHookAvailable)
                overlays.Add(new MouseKeyHook(Settings.MouseClicks, Settings.KeyStrokes));

            if (Settings.IncludeCursor)
                overlays.Add(MouseCursor.Instance);

            if (overlays.Count > 0)
                return new OverlayedImageProvider(imageProvider, offset, overlays.ToArray());

            return imageProvider;
        }
        
        public async Task StopRecording()
        {
            Status.LocalizationKey = nameof(Resources.Stopped);

            var savingRecentItem = RecentViewModel.Add(_currentFileName, isVideo ? RecentItemType.Video : RecentItemType.Audio, true);
            
            RecorderState = RecorderState.NotRecording;

            // Set Recorder to null
            var rec = _recorder;
            _recorder = null;

            var task = Task.Run(() => rec.Dispose());

            _timer?.Stop();
            _timing.Stop();

            #region After Recording Tasks
            CanChangeVideoSource = true;
            
            if (Settings.MinimizeOnStart)
                ServiceProvider.Get<Action<bool>>(ServiceName.Minimize).Invoke(false);

            VideoViewModel.RegionProvider.Release();
            #endregion

            // Ensure saved
            await task;
            
            // After Save
            savingRecentItem.Saved();

            if (Settings.CopyOutPathToClipboard)
                _currentFileName.WriteToClipboard();
            
            ServiceProvider.SystemTray.ShowTextNotification((isVideo ? Resources.VideoSaved : Resources.AudioSaved) + ": " + Path.GetFileName(_currentFileName), 5000, () =>
            {
                ServiceProvider.LaunchFile(new ProcessStartInfo(_currentFileName));
            });
        }
    }
}