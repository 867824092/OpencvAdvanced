using System;
using System.IO;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ReactiveUI;

namespace OpencvDesktop.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    private static readonly string HaarXml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
        "haarcascade_frontalface_default.xml");

    private string _imagePath;
    public string ImagePath {
        get => _imagePath;
        set => this.RaiseAndSetIfChanged(ref _imagePath, value);
    }

    private Bitmap? _bitmap;
    public Bitmap? Bitmap {
        get => _bitmap;
        set => this.RaiseAndSetIfChanged(ref _bitmap, value);
    }

    private int _hmin;
    public int Hmin {
        get => _hmin;
        set => this.RaiseAndSetIfChanged(ref _hmin, value);
    }

    private int _smin;
    public int Smin {
        get => _smin;
        set => this.RaiseAndSetIfChanged(ref _smin, value);
    }
    
    private int _vmin;
    public int Vmin {
        get => _vmin;
        set => this.RaiseAndSetIfChanged(ref _vmin, value);
    }
    
    private int _hmax;
    public int Hmax {
        get => _hmax;
        set => this.RaiseAndSetIfChanged(ref _hmax, value);
    }
    
    private int _smax;
    public int Smax {
        get => _smax;
        set => this.RaiseAndSetIfChanged(ref _smax, value);
    }
    
    private int _vmax;
    public int Vmax {
        get => _vmax;
        set => this.RaiseAndSetIfChanged(ref _vmax, value);
    }

    public ReactiveCommand<ColorConversionCodes, Task> ChangeBitmapColorCommand { get; }
    public ReactiveCommand<Unit, Task> DetectFacesCommand { get; }
    public ReactiveCommand<Unit, Task> UpdateBackgroundcolorCommand { get; }
    public ReactiveCommand<Unit, Task> UpdateResolutionRatioCommand { get; }
    public ReactiveCommand<Unit,Task> CollectVideoCommand { get; }
    public MainWindowViewModel() {
        ChangeBitmapColorCommand = ReactiveCommand.Create<ColorConversionCodes, Task>(ChangeBitmapColor);
        DetectFacesCommand = ReactiveCommand.Create(DetectFaces);
        UpdateBackgroundcolorCommand = ReactiveCommand.Create(UpdatePhotoBackgroundcolor);
        UpdateResolutionRatioCommand = ReactiveCommand.Create(UpdateResolutionRatio);
        CollectVideoCommand = ReactiveCommand.Create(CollectVideoImg);
    }

    /// <summary>
    /// 改变图片颜色
    /// </summary>
    private Task ChangeBitmapColor(ColorConversionCodes colorConversionCodes) {
        CheckBitMap();
        using Mat mat = new Mat(ImagePath, ImreadModes.Color);
        using Mat grayImage = new Mat();
        Cv2.CvtColor(mat, grayImage, colorConversionCodes);
        System.Drawing.Bitmap bitmap = grayImage.ToBitmap();
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
        stream.Seek(0, SeekOrigin.Begin);
        Bitmap = new Avalonia.Media.Imaging.Bitmap(stream);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 人脸检测
    /// </summary>
    private Task DetectFaces() {
        CheckBitMap();
        using Mat mat = new Mat(ImagePath, ImreadModes.Color);
        //haar分类器
        CascadeClassifier faceCascade = new CascadeClassifier(HaarXml);
        //检测人脸
        Rect[] faces = faceCascade.DetectMultiScale(mat,
            1.1, //缩放比例
            3, //最小邻居数
            HaarDetectionTypes.DoRoughSearch, //检测类型
            new Size(20, 20)); //最小人脸
        foreach (Rect face in faces) {
            // 在图像上绘制矩形来标记人脸区域
            Cv2.Rectangle(mat, face, new Scalar(255, 0, 0), 2);
        }

        System.Drawing.Bitmap bitmap = mat.ToBitmap();
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
        stream.Seek(0, SeekOrigin.Begin);
        Bitmap = new Avalonia.Media.Imaging.Bitmap(stream);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 修改证件照底色
    /// </summary>
    /// <returns></returns>
    private Task UpdatePhotoBackgroundcolor() {
        CheckBitMap();
        using Mat mat = Cv2.ImRead(ImagePath);
        using Mat hsv = new Mat();
        //转为hsv灰度图像
        Cv2.CvtColor(mat, hsv, ColorConversionCodes.BGR2HSV);
        Cv2.ImShow("hsv", hsv);
        using Mat binaryImage = new Mat();
        Scalar lower = new Scalar(_hmin, _smin, _vmin);  
        Scalar upper = new Scalar(_hmax, _smax, _vmax);
        //图片二值化处理
        Cv2.InRange(hsv, lower , upper , binaryImage);
        Cv2.ImShow("binary", binaryImage);
        //腐蚀膨胀
        Mat erode = new Mat();
        Cv2.Erode(binaryImage,erode, 1);
        Mat dilate = new Mat();
        Cv2.Dilate(erode, dilate, 1);
        Cv2.ImShow("dilate.png", dilate);
        
        using Mat copy = mat.Clone(); 
        for (int r = 0; r < copy.Rows; r++)
        {
            for (int c = 0; c < copy.Cols; c++)
            {
                if (dilate.At<byte>(r, c) == 255)
                {
                    Vec3b vec3B = dilate.At<Vec3b>(r, c);
                    //此处替换颜色，为BGR通道，不是RGB通道
                    vec3B.Item0 = 0;
                    vec3B.Item1 = 0;
                    vec3B.Item2 = 255;
                    copy.Set<Vec3b>(r, c, vec3B);
                }
            }
        }
        Cv2.ImShow("new.png", copy);
        System.Drawing.Bitmap bitmap = copy.ToBitmap();
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
        stream.Seek(0, SeekOrigin.Begin);
        Bitmap = new Bitmap(stream);
        return Task.CompletedTask;
    }
    /// <summary>
    /// 修改图片分辨率
    /// </summary>
    /// <returns></returns>
    private Task UpdateResolutionRatio() {
        using Mat mat = new Mat(ImagePath, ImreadModes.Color);
        using Mat newMat = mat.Clone();
        Cv2.Resize(mat, newMat, new Size(1920, 1080));
        System.Drawing.Bitmap bitmap = newMat.ToBitmap();
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
        stream.Seek(0, SeekOrigin.Begin);
        Bitmap = new Avalonia.Media.Imaging.Bitmap(stream);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 采集摄像头图片
    /// </summary>
    /// <returns></returns>
    private Task CollectVideoImg() {
        VideoCapture capture = new VideoCapture(0);
        using (Mat image = new Mat()) // Frame image buffer
        {
            capture.Read(image); // same as cvQueryFrame
            if (!image.Empty()) {
                System.Drawing.Bitmap bitmap = image.ToBitmap();
                using var stream = new MemoryStream();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                stream.Seek(0, SeekOrigin.Begin);
                Bitmap = new Bitmap(stream);
            }
        }
        return Task.CompletedTask;
    }

    private void CheckBitMap() {
        this.Bitmap?.Dispose();
        this.Bitmap = null;
    }
}
