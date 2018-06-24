using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Graphics.Imaging;
using System.ComponentModel;
using Windows.ApplicationModel.Resources;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Diarix.ContentDialogs
{
    /// <summary>
    /// 画像の縮小およびトリミング機能を提供するダイアログです．
    /// </summary>
    public sealed partial class ImageTrimmer : ContentDialog, INotifyPropertyChanged
    {
        private struct IntPoint
        {
            public int X;
            public int Y;
            public IntPoint(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
            public IntPoint(Point point)
            {
                this.X = (int)point.X;
                this.Y = (int)point.Y;
            }
            public bool IsDefault() => this.X == 0 && this.Y == 0;
        }
        private static readonly ResourceLoader resw = ResourceLoader.GetForCurrentView();
        /// <summary>
        /// 保存領域を選択するとき，スクロールバーの自動移動が必要になった場合，一度にこの量だけ移動する．
        /// </summary>
        private static readonly int scrollViewerOffsetChange = 15;
        /// <summary>
        /// このクラスがサポートする画像ファイルの拡張子．このフィールドは読み取り専用です．
        /// </summary>
        public static readonly string[] SupportedImageExtensions = { ".bmp", ".png", ".jpg", ".jpeg" };
        private int _magnification;
        private string _explanationText;
        /// <summary>
        /// 画像の拡大率を取得または設定します．
        /// </summary>
        private int magnification
        {
            get => this._magnification;
            set
            {
                if (this.magnification == value) return;
                var beforeMagnification = value;
                this._magnification = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.magnification)));
                this.Image.Width = (int)(this.bitmapImage.PixelWidth * value / 100.0);
                this.Image.Height = (int)(this.bitmapImage.PixelHeight * value / 100.0);
                this.SetSelectedArea(this.trimEnd);
                this.SetExplanationText();
            }
        }
        private string explanationText
        {
            get => this._explanationText;
            set
            {
                this._explanationText = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.explanationText)));
            }
        }
        /// <summary>
        /// ポインタが押されているかどうか．
        /// </summary>
        private bool isPointerPressing;
        /// <summary>
        /// このダイアログの呼び出し元から指定された画像．
        /// </summary>
        private BitmapImage bitmapImage;
        /// <summary>
        /// 画像の縮小およびトリミングに使用するデコーダ．
        /// </summary>
        private BitmapDecoder decoder;
        /// <summary>
        /// トリミングの始点位置．
        /// </summary>
        private IntPoint trimStart;
        /// <summary>
        /// トリミングの終点位置．
        /// </summary>
        private IntPoint trimEnd;
        /// <summary>
        /// 縮小，トリミングされた画像．
        /// </summary>
        public WriteableBitmap TrimmedBitmap { get; private set; }
        /// <summary>
        /// ImageTrimmerダイアログの新しいインスタンスを初期化します．
        /// </summary>
        public ImageTrimmer()
        {
            this.InitializeComponent();
            this._magnification = 100;
            this.isPointerPressing = false;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void SetExplanationText()
        {
            var initialSizeText = $"{resw.GetString("/ContentDialogs/InitialSize")} {this.bitmapImage?.PixelWidth ?? 0}×{this.bitmapImage?.PixelHeight ?? 0}";
            var selectingSizeText = $"{resw.GetString("/ContentDialogs/SelectedSize")} { Math.Abs(this.trimEnd.X - this.trimStart.X)}×{ Math.Abs(this.trimEnd.Y - this.trimStart.Y)}";
            this.explanationText = $"{resw.GetString("/ContentDialogs/Magnification")} {this.magnification}%\n{selectingSizeText}\n{initialSizeText}";
        }
        /// <summary>
        /// 指定した画像ファイルを縮小・トリミングの対象にします．
        /// </summary>
        /// <param name="ImageStorageFile"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task SetSourceAsync(IStorageFile ImageStorageFile)
        {
            if (!SupportedImageExtensions.Contains(Path.GetExtension(ImageStorageFile.Name).ToLower()))
                throw new ArgumentException($"{ImageStorageFile.Name}is unsupported kind of file.");
            this.bitmapImage = new BitmapImage();
            using (var stream = await ImageStorageFile.OpenReadAsync())
            {
                await bitmapImage.SetSourceAsync(stream);
                this.decoder = await BitmapDecoder.CreateAsync(stream);
            }
            this.Image.Source = bitmapImage;
            this.Image.Width = bitmapImage.PixelWidth;
            this.Image.Height = bitmapImage.PixelHeight;
            this.trimStart = new IntPoint();
            this.trimEnd = new IntPoint();
            this.magnification = 100;
            this.SetExplanationText();
        }
        /// <summary>
        /// 選択領域を更新します．
        /// </summary>
        /// <param name="pointer">キャンバス左上を基準とした，マウスポインタの座標．</param>
        private void SetSelectedArea(IntPoint pointer)
        {
            //マウスポインタが画像領域からはみ出ないようにする
            if (pointer.X > this.bitmapImage.PixelWidth * this.magnification / 100.0) pointer.X = (int)(this.bitmapImage.PixelWidth * this.magnification / 100.0);
            if (pointer.Y > this.bitmapImage.PixelHeight * this.magnification / 100.0) pointer.Y = (int)(this.bitmapImage.PixelHeight * this.magnification / 100.0);
            //
            var leftMargin = Math.Min(this.trimStart.X, pointer.X);
            var topMargin = Math.Min(this.trimStart.Y, pointer.Y);
            var width = Math.Abs(this.trimStart.X - pointer.X);
            var height = Math.Abs(this.trimStart.Y - pointer.Y);
            this.trimEnd = pointer;
            this.SelectedArea.Margin = new Thickness(leftMargin, topMargin, 0, 0);
            this.SelectedArea.Width = width;
            this.SelectedArea.Height = height;
        }
        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            //領域を指定されなかった場合は全面を保存
            if (this.trimStart.IsDefault() && this.trimEnd.IsDefault())
            {
                this.trimEnd = new IntPoint((int)(this.bitmapImage.PixelWidth * this.magnification / 100.0), (int)(this.bitmapImage.PixelHeight * this.magnification / 100.0));
            }
            //指定された領域の幅か高さが0の場合は保存しない
            if (this.trimEnd.X - this.trimStart.X == 0 || this.trimEnd.Y - this.trimStart.Y == 0)
            {
                this.explanationText = resw.GetString("/ContentDialogs/InvalidTrim");
                args.Cancel = true;
                return;
            }
            //
            BitmapBounds bounds = new BitmapBounds()
            {
                X = (uint)(Math.Min(this.trimStart.X, this.trimEnd.X)),
                Y = (uint)(Math.Min(this.trimStart.Y, this.trimEnd.Y)),
                Width = (uint)(Math.Abs(this.trimEnd.X - this.trimStart.X)),
                Height = (uint)(Math.Abs(this.trimEnd.Y - this.trimStart.Y)),
            };
            BitmapTransform transform = new BitmapTransform()
            {
                Bounds = bounds,
                ScaledWidth = (uint)(this.bitmapImage.PixelWidth * this.magnification / 100.0),
                ScaledHeight = (uint)(this.bitmapImage.PixelHeight * this.magnification / 100.0),
            };
            var provider = await this.decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, transform, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
            var pixels = provider.DetachPixelData();
            this.TrimmedBitmap = new WriteableBitmap((int)bounds.Width, (int)bounds.Height);
            using (var stream = this.TrimmedBitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(pixels, 0, pixels.Length);
            }
        }
        private void Image_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.isPointerPressing = true;
            var pointer = new IntPoint(e.GetCurrentPoint(sender as UIElement).Position);
            //マウスポインタが画面からはみ出ないようにする
            if (pointer.X > this.bitmapImage.PixelWidth * this.magnification / 100.0) pointer.X = (int)(this.bitmapImage.PixelWidth * this.magnification / 100.0);
            if (pointer.Y > this.bitmapImage.PixelHeight * this.magnification / 100.0) pointer.Y = (int)(this.bitmapImage.PixelHeight * this.magnification / 100.0);
            //
            this.trimStart = pointer;
            this.SelectedArea.Margin = new Thickness(this.trimStart.X, this.trimStart.Y, 0, 0);
            this.SelectedArea.Width = 0;
            this.SelectedArea.Height = 0;
            this.SetExplanationText();
            e.Handled = true;
        }
        private void Image_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!this.isPointerPressing) return;
            var pointer = new IntPoint(e.GetCurrentPoint(sender as UIElement).Position);
            //マウスポインタがScrollViewerの端近くに来た場合，自動で少しスクロール位置をずらす
            if (pointer.X - 10 < this.ScrollViewer.HorizontalOffset)
                this.ScrollViewer.ChangeView(this.ScrollViewer.HorizontalOffset - scrollViewerOffsetChange, this.ScrollViewer.VerticalOffset, 1);
            if (pointer.Y - 10 < this.ScrollViewer.VerticalOffset)
                this.ScrollViewer.ChangeView(this.ScrollViewer.HorizontalOffset, this.ScrollViewer.VerticalOffset - scrollViewerOffsetChange, 1);
            if (pointer.X + 25 > this.ScrollViewer.ActualWidth + this.ScrollViewer.HorizontalOffset)
                this.ScrollViewer.ChangeView(this.ScrollViewer.HorizontalOffset + scrollViewerOffsetChange, this.ScrollViewer.VerticalOffset, 1);
            if (pointer.Y + 25 > this.ScrollViewer.ActualHeight + this.ScrollViewer.VerticalOffset)
                this.ScrollViewer.ChangeView(this.ScrollViewer.HorizontalOffset, this.ScrollViewer.VerticalOffset + scrollViewerOffsetChange, 1);
            //
            this.SetSelectedArea(new IntPoint(e.GetCurrentPoint(sender as UIElement).Position));
            this.SetExplanationText();
            e.Handled = true;
        }
        private void Image_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            this.isPointerPressing = false;
            this.SetSelectedArea(new IntPoint(e.GetCurrentPoint(sender as UIElement).Position));
            this.SetExplanationText();
            e.Handled = true;
        }
        private void Expand_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.magnification = Math.Min(this.magnification + 5, 100);
            this.SetExplanationText();
        }
        private void Contract_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.magnification = Math.Max(this.magnification - 5, 10);
            this.SetExplanationText();
        }
    }
}
