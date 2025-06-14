using FaceDetectionApp.Helpers;
using Microsoft.Win32;
using OpenCvSharp;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FaceDetectionApp.Models;


namespace FaceDetectionApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private WriteableBitmap _currentFrame;
        public WriteableBitmap CurrentFrame
        {
            get => _currentFrame;
            set
            {
                _currentFrame = value;
                OnPropertyChanged(nameof(CurrentFrame));
            }
        }

        private string _statusMessage = "준비 완료";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public ICommand LoadImageCommand { get; }

        private CancellationTokenSource _playbackCts;


        private int _webcamIndex = 0;
        public int WebcamIndex
        {
            get => _webcamIndex;
            set
            {
                _webcamIndex = value;
                OnPropertyChanged(nameof(WebcamIndex));
            }
        }

        public MainViewModel()
        {
            LoadImageCommand = new RelayCommand(LoadImage);
            StartWebcamCommand = new RelayCommand(StartWebcam);
            StopWebcamCommand = new RelayCommand(StopWebcam);

            StartFlaskServer();
            ShowImage("default_idle.jpg");
        }

        //이미지 선택 함수
        private async void LoadImage()
        {
            StopWebcam();
            var dialog = new OpenFileDialog
            {
                Filter = "미디어 파일 (*.png;*.jpg;*.jpeg;*.mp4;*.avi)|*.png;*.jpg;*.jpeg;*.mp4;*.avi"

            };

            if (dialog.ShowDialog() != true) return;

            var model = new FaceDetectionModel
            {
                InputPath = dialog.FileName,
                IsVideo = Path.GetExtension(dialog.FileName).ToLower() is ".mp4" or ".avi"
            };

            if (model.IsVideo)
            {
                model.ResultPath = await ProcessAndSaveVideoAsync(model);
                if (model.ResultPath != null)
                    await PlaySavedVideoAsync(model.ResultPath);
            }
            else
            {
                _playbackCts?.Cancel();
                await ProcessImageAsync(model);
            }

        }

        // 이미지 선택 시 처리
        private async Task ProcessImageAsync(FaceDetectionModel model)

        {
            using var inputMat = Cv2.ImRead(model.InputPath);
            var coordsList = await GetNoseCoordinatesAsync(model.InputPath);
            model.Noses = coordsList.Select(c => new DetectedNose { X = c.X, Y = c.Y, Width = c.Width }).ToList();


            if (model.Noses.Count > 0)
            {
                string nosePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "rudolph_nose.png");
                using var noseMat = Cv2.ImRead(nosePath, ImreadModes.Unchanged);

                foreach (var nose in model.Noses)
                {
                    if (nose.Width <= 0) continue;
                    using var resized = new Mat();
                    Cv2.Resize(noseMat, resized, new OpenCvSharp.Size(nose.Width, nose.Width));
                    OverlayImage(inputMat, resized, nose.X, nose.Y);
                }
            }

            model.ResultPath = GetRudolphOutputPath(model.InputPath);
            Cv2.ImWrite(model.ResultPath, inputMat);
            StatusMessage = $"이미지 저장 완료: {model.ResultPath}";


            CurrentFrame = ConvertToWriteableBitmap(inputMat);
        }

        // 비디오 선택 시 처리
        private async Task<string> ProcessAndSaveVideoAsync(FaceDetectionModel model)
        {
            using var capture = new VideoCapture(model.InputPath);
            model.ResultPath = GetRudolphOutputPath(model.InputPath);
            if (!capture.IsOpened())
            {
                StatusMessage = "원본 영상 열기에 실패했습니다.";
                return null;
            }

            int fourcc = VideoWriter.FourCC('M', 'J', 'P', 'G');
            double fps = capture.Fps;
            int width = (int)capture.FrameWidth;
            int height = (int)capture.FrameHeight;

            using var writer = new VideoWriter(model.ResultPath, fourcc, fps, new OpenCvSharp.Size(width, height));
            if (!writer.IsOpened())
            {
                StatusMessage = "영상 저장에 실패했습니다.";
                return null;
            }

            var nosePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "rudolph_nose.png");
            var noseMat = Cv2.ImRead(nosePath, ImreadModes.Unchanged);

            var frame = new Mat();
            while (capture.Read(frame))
            {
                if (frame.Empty()) break;

                var frameCopy = frame.Clone();
                using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame);
                var coordsList = await GetNoseCoordinatesAsync(bitmap);
                model.Noses = coordsList.Select(c => new DetectedNose { X = c.X, Y = c.Y, Width = c.Width }).ToList();

                foreach (var nose in model.Noses)
                {
                    if (nose.Width <= 0) continue;
                    using var resized = new Mat();
                    Cv2.Resize(noseMat, resized, new OpenCvSharp.Size(nose.Width, nose.Width));
                    OverlayImage(frameCopy, resized, nose.X, nose.Y);
                }

                writer.Write(frameCopy);
            }

            StatusMessage = $"영상 저장 완료: {model.ResultPath}";
            return model.ResultPath;

        }

        // 저장된 비디오 화면 표출
        private async Task PlaySavedVideoAsync(string savedPath)
        {
            _playbackCts?.Cancel();
            _playbackCts = new CancellationTokenSource();
            var token = _playbackCts.Token;

            using var capture = new VideoCapture(savedPath);
            if (!capture.IsOpened())
            {
                StatusMessage = "저장된 영상을 열 수 없습니다.";
                return;
            }

            var frame = new Mat();
            while (capture.Read(frame))
            {
                if (frame.Empty() || token.IsCancellationRequested) break;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentFrame = ConvertToWriteableBitmap(frame);
                });

                await Task.Delay(33, token);
            }

            frame.Dispose();
            StatusMessage = "영상 출력 종료";
        }



        // 저장위치 및 파일 이름 형성
        private string GetRudolphOutputPath(string originalPath)
        {
            string resultDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "result");
            Directory.CreateDirectory(resultDir);

            string filename = Path.GetFileNameWithoutExtension(originalPath);
            string ext = Path.GetExtension(originalPath);
            return Path.Combine(resultDir, $"{filename}_rudolph{ext}");
        }

        //
        private WriteableBitmap ConvertToWriteableBitmap(Mat mat)
        {
            var wb = new WriteableBitmap(mat.Width, mat.Height, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);
            wb.Lock();

            using var convertedMat = (mat.Type() != MatType.CV_8UC3)
                ? mat.CvtColor(ColorConversionCodes.GRAY2BGR)
                : mat.Clone();

            int stride = wb.BackBufferStride;
            int bytes = stride * wb.PixelHeight;
            byte[] buffer = new byte[bytes];

            Marshal.Copy(convertedMat.Data, buffer, 0, buffer.Length);
            Marshal.Copy(buffer, 0, wb.BackBuffer, buffer.Length);

            wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
            wb.Unlock();

            return wb;
        }

        // 이미지 선택 시 코 위치 추출 요청(파라미터 이미지 경로)
        private async Task<List<(int X, int Y, int Width)>> GetNoseCoordinatesAsync(string imagePath)
        {
            try
            {
                using var mat = Cv2.ImRead(imagePath);
                if (mat.Empty())
                {
                    StatusMessage = "이미지 로딩 실패";
                    return new List<(int, int, int)>();
                }

                using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
                return await NoseDetectorClient.DetectNosesFromBitmapAsync(bitmap);
            }
            catch (Exception ex)
            {
                StatusMessage = $"코 추출 실패: {ex.Message}";
                return new List<(int, int, int)>();
            }
        }


        // 영상 코 위치 추출 요청 (직접 비트맵 전달)
        private async Task<List<(int X, int Y, int Width)>> GetNoseCoordinatesAsync(Bitmap bitmap)
        {
            try
            {
                return await NoseDetectorClient.DetectNosesFromBitmapAsync(bitmap);
            }
            catch (Exception ex)
            {
                StatusMessage = $"코 추출 실패: {ex.Message}";
                return new List<(int, int, int)>();
            }
        }


        // 루돌프 코 합성
        private void OverlayImage(Mat background, Mat overlay, int centerX, int centerY)
        {
            int x = centerX - overlay.Width / 2;
            int y = centerY - overlay.Height / 2;

            for (int i = 0; i < overlay.Rows; i++)
            {
                for (int j = 0; j < overlay.Cols; j++)
                {
                    int bgY = y + i;
                    int bgX = x + j;

                    if (bgY < 0 || bgY >= background.Rows || bgX < 0 || bgX >= background.Cols)
                        continue;

                    Vec4b overlayPixel = overlay.At<Vec4b>(i, j);
                    byte alpha = overlayPixel.Item3;

                    if (alpha == 0) continue;

                    Vec3b bgPixel = background.At<Vec3b>(bgY, bgX);

                    float a = alpha / 255f;
                    for (int c = 0; c < 3; c++)
                    {
                        bgPixel[c] = (byte)(overlayPixel[c] * a + bgPixel[c] * (1 - a));
                    }

                    background.Set(bgY, bgX, bgPixel);
                }
            }
        }

        // 파이선 서버 관리
        private Process _flaskServerProcess;

        private void StartFlaskServer()
        {
            try
            {
                var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Detector", "nose_detection_server.exe");

                if (!File.Exists(exePath))
                {
                    StatusMessage = "Flask 서버 실행 파일이 없습니다.";
                    return;
                }

                foreach (var proc in Process.GetProcessesByName("nose_detection_server"))
                {
                    try
                    {
                        proc.Kill(true);
                        proc.WaitForExit();
                    }
                    catch { }
                }

                _flaskServerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                };

                _flaskServerProcess.Start();
                StatusMessage = "Flask 서버 실행됨";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Flask 서버 실행 실패: {ex.Message}";
            }
        }



        public void DisposeFlaskServer()
        {
            try
            {
                if (_flaskServerProcess != null && !_flaskServerProcess.HasExited)
                {
                    _flaskServerProcess.Kill(true);
                    _flaskServerProcess.Dispose();
                    _flaskServerProcess = null;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Flask 서버 종료 실패: {ex.Message}";
            }
        }

        // 웹캠 관리
        private VideoCapture _webcam;
        private CancellationTokenSource _webcamCts;
        private bool _isWebcamRunning = false;

        public ICommand StartWebcamCommand { get; }
        public ICommand StopWebcamCommand { get; }


        private async void StartWebcam()
        {
            if (_isWebcamRunning) return;

            ShowImage("webcam_loading.jpg");
            StatusMessage = "웹캠 로딩 중...";

            var opened = await Task.Run(() =>
            {
                _webcam = new VideoCapture(WebcamIndex);
                return _webcam.IsOpened();
            });

            if (!opened)
            {
                StatusMessage = "웹캠을 열 수 없습니다.";
                return;
            }


            _isWebcamRunning = true;
            _webcamCts = new CancellationTokenSource();



            await Task.Run(async () =>
            {
                var mat = new Mat();

                while (!_webcamCts.Token.IsCancellationRequested)
                {
                    _webcam.Read(mat);
                    if (!mat.Empty())
                    {
                        var model = new FaceDetectionModel();  // 웹캠용 Model 생성
                        using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
                        var coordsList = await GetNoseCoordinatesAsync(bitmap);

                        model.Noses = coordsList.Select(c => new DetectedNose { X = c.X, Y = c.Y, Width = c.Width }).ToList();

                        if (model.Noses.Count > 0)
                        {
                            using var overlay = Cv2.ImRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "rudolph_nose.png"), ImreadModes.Unchanged);

                            foreach (var nose in model.Noses)
                            {
                                if (nose.Width <= 0) continue;
                                using var resized = new Mat();
                                Cv2.Resize(overlay, resized, new OpenCvSharp.Size(nose.Width, nose.Width));
                                OverlayImage(mat, resized, nose.X, nose.Y);
                            }
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CurrentFrame = ConvertToWriteableBitmap(mat);
                        });
                    }

                    await Task.Delay(33);
                }


                mat.Dispose();
            });

        }

        private void StopWebcam()
        {
            if (!_isWebcamRunning) return;

            _webcamCts.Cancel();
            _webcam.Release();
            _webcam.Dispose();
            _isWebcamRunning = false;
            StatusMessage = "웹캠 중지됨";
        }

        //이미지 표출 함수
        private void ShowImage(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", fileName);
            if (File.Exists(path))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(path);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                var writeable = new WriteableBitmap(bitmapImage);
                CurrentFrame = writeable;
            }
        }




        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}