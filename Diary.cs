using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Media.Core;
using System.IO;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Input;
using Windows.System;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.UI.Core;
using Diarix.UserControls;

namespace Diarix
{
    class DailyDateTimeComparer : IEqualityComparer<DateTime>
    {
        public bool Equals(DateTime date1, DateTime date2) =>
            date1.Year == date2.Year && date1.Month == date2.Month && date1.Day == date2.Day;
        public int GetHashCode(DateTime date) => date.GetHashCode();
    }
    /// <summary>
    /// このアプリの設定を管理します．
    /// </summary>
    class SettingFileset
    {
        private static readonly string settingFilesetName = "settings.dat";
        private static readonly byte[] cryptoKey = Encoding.Unicode.GetBytes("じゃ８４ｃｑｈｘ9だｖｙｆヴぁｈｆ");
        private byte[] salt;
        private byte[] hash;
        /// <summary>
        /// 日記を開くためのキーを取得します．
        /// </summary>
        public byte[] DiaryFilesetKey => this.hash;
        /// <summary>
        /// アイコンなどをコンパクトに表示するか取得または設定します．
        /// </summary>
        public bool IsCompactModeEnabled { get; set; }
        /// <summary>
        /// アプリ起動時にパスワード入力をスキップするか取得または設定します．
        /// </summary>
        public bool IsSkipPasswordInput { get; set; }
        private SettingFileset() { }
        /// <summary>
        /// 設定ファイルが存在するか返します．
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> ExistSettingFilesetAsync()
            => await ApplicationStorage.Exists(settingFilesetName);
        /// <summary>
        /// 設定ファイルを開きます．設定ファイルが存在しない場合は，規定の設定で新規作成されます．
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public static async Task<SettingFileset> OpenOrCreateSettingFilesetAsync()
        {
            SettingFileset value = new SettingFileset();
            using (var cryptoFileset = await CryptoFileset.OpenOrCreateCryptoFilesetAsync(settingFilesetName, cryptoKey, true))
            {
                try
                {
                    value.salt = await cryptoFileset.ReadAsync(nameof(value.salt));
                    value.hash = await cryptoFileset.ReadAsync(nameof(value.hash));
                    value.IsCompactModeEnabled = BitConverter.ToBoolean(await cryptoFileset.ReadAsync(nameof(value.IsCompactModeEnabled)), 0);
                    value.IsSkipPasswordInput = BitConverter.ToBoolean(await cryptoFileset.ReadAsync(nameof(value.IsSkipPasswordInput)), 0);
                }
                catch
                {
                    value.salt = new byte[0];
                    value.hash = new byte[0];
                    value.IsCompactModeEnabled = false;
                    value.IsSkipPasswordInput = false;
                }
            }
            return value;
        }
        /// <summary>
        /// 日記を開くためのパスワードを変更します．
        /// </summary>
        /// <param name="newPassword"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException"></exception>
        public void SetPassword(string newPassword)
        {
            if (newPassword == null) throw new ArgumentNullException(nameof(newPassword));
            if (!newPassword.Any()) throw new ArgumentException($"{nameof(newPassword)} must have one character at least.");
            PasswordEncoder encoder = new PasswordEncoder(newPassword);
            this.salt = encoder.Salt;
            this.hash = encoder.Hash;
        }
        /// <summary>
        /// 指定したパスワードが日記を開くための正しいパスワードかどうか返します．
        /// </summary>
        /// <param name="inputedPassword"></param>
        /// <returns></returns>
        public bool IsCorrectPassword(string inputedPassword)
        {
            if (inputedPassword == null || !inputedPassword.Any()) return false;
            var inputedHash = PasswordEncoder.CreateHash(inputedPassword, this.salt);
            return inputedHash.Length == this.hash.Length && Enumerable.Range(0, this.hash.Length).All(i => inputedHash[i] == this.hash[i]);
        }
        /// <summary>
        /// 設定の変更点をすべて保存します．
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="IOException"></exception>
        public async Task FlushAsync()
        {
            using (var cryptoFileset = await CryptoFileset.OpenOrCreateCryptoFilesetAsync(settingFilesetName, cryptoKey, true))
            {
                await cryptoFileset.WriteAsync(nameof(this.salt), this.salt, true);
                await cryptoFileset.WriteAsync(nameof(this.hash), this.hash, true);
                await cryptoFileset.WriteAsync(nameof(this.IsCompactModeEnabled), BitConverter.GetBytes(this.IsCompactModeEnabled), true);
                await cryptoFileset.WriteAsync(nameof(this.IsSkipPasswordInput), BitConverter.GetBytes(this.IsSkipPasswordInput), true);
                await cryptoFileset.FlushAsync();
            }
        }
    }
    /// <summary>
    /// 日記ファイルセットに対する読み込みおよび書き込み機能を提供します．
    /// </summary>
    public class DiaryFileset : Disposable
    {
        /// <summary>
        /// 文字列の読み込みおよび書き込みに使用するエンコーディング．このフィールドは読み取り専用です．
        /// </summary>
        private static readonly Encoding encoding = Encoding.Unicode;
        /// <summary>
        /// サポートされている画像ファイルの拡張子．このフィールドは読み取り専用です．
        /// </summary>
        public static readonly string[] SupportedImageExtensions = { ".bmp", ".png", ".jpg", ".jpeg", ".gif" };
        /// <summary>
        /// サポートされている音楽・動画ファイルの拡張子．このフィールドは読み取り専用です．
        /// </summary>
        public static readonly string[] SupportedMediaExtenions = { ".mp3" };
        /// <summary>
        /// 暗号化された日記ファイルセットに対して読み込み，書き込みを行う
        /// </summary>
        private CryptoFileset cryptoFileset;
        /// <summary>
        /// この日記の暗号化および複合化に使用するキーを取得します．
        /// </summary>
        public byte[] Key
        {
            get; private set;
        }
        /// <summary>
        /// 現在操作の対象となっている日記の日付を取得します．
        /// </summary>
        public DateTime Date
        {
            get => new DateTime(this.Year, this.Month, this.Day, 0, 0, 0);
            private set
            {
                this.Year = value.Year;
                this.Month = value.Month;
                this.Day = value.Day;
            }
        }
        public int Year { get; private set; }
        public int Month { get; private set; }
        public int Day { get; private set; }
        /// <summary>
        /// 指定したキーを用いてDiaryFilesetクラスの新しいインスタンスを初期化します．
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentException">長さ0のキーを設定することはできません．</exception>
        /// <exception cref="ArgumentNullException">keyがnullです．</exception>
        private DiaryFileset(byte[] key)
        {
            this.Key = key ?? throw new ArgumentNullException(nameof(key));
            if (!key.Any()) throw new ArgumentException($"{nameof(key)} must have one element at least.");
            this.Date = DateTime.MinValue;
        }
        /// <summary>
        /// 指定したキーと日付に基づいて，日記ファイルセットを非同期的に開いて返します．
        /// </summary>
        /// <param name="date"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">長さ0のキーを設定することはできません．</exception>
        /// <exception cref="ArgumentNullException">keyがnullです．</exception>
        /// <exception cref="IOException"></exception>
        public static async Task<DiaryFileset> OpenDiaryFilesetAsync(DateTime date, byte[] key)
        {
            return new DiaryFileset(key)
            {
                cryptoFileset = await CryptoFileset.OpenOrCreateCryptoFilesetAsync(GetDiaryFilesetFolderName(date), GetDiaryFilesetName(date), key, true),
                Date = date
            };
        }
        private static string GetDiaryFilesetFolderName(DateTime date) => date.Year.ToString();
        private static string GetDiaryFilesetName(DateTime date) => date.Month + ".diary";
        private static string GetDiaryName(DateTime date) => date.Day + ".diary";
        /// <summary>
        /// すでに保存されているファイルと被らないファイル名を返します．
        /// </summary>
        /// <param name="DesiredFilename"></param>
        /// <returns></returns>
        public string GetIdentifiableFilename(string DesiredFilename)
        {
            //もともとのファイル名と同じ名前のファイルがファイルセットに存在しない場合は，引数をそのまま返す．
            if (!this.cryptoFileset.Exists(DesiredFilename)) return DesiredFilename;
            var withoutExtension = Path.GetFileNameWithoutExtension(DesiredFilename);
            var extension = Path.GetExtension(DesiredFilename);
            //1つずつカウントアップしていき，名前が被らないものが出てきたらそれを返す．
            int index = 1;
            while (this.cryptoFileset.Exists(withoutExtension + "(" + index + ")" + extension)) index++;
            return withoutExtension + "(" + index + ")" + extension;
        }
        /// <summary>
        /// 現在開いているファイルセットに保存されている日記の日付を返します．
        /// </summary>
        /// <returns></returns>
        public IOrderedEnumerable<DateTime> GetExistingDiaryDates(bool IncludeEmptyDiary) =>
                from date in Enumerable.Range(1, DateTime.DaysInMonth(this.Year, this.Month)).Select((day) => new DateTime(this.Year, this.Month, day))
                where this.cryptoFileset.Exists(GetDiaryName(date))
                where IncludeEmptyDiary ? true : this.cryptoFileset.Sizeof(GetDiaryName(date)) > 0
                orderby date.Day
                select date;
        /// <summary>
        /// 現在開いているファイルセットに保存されている日記の日付を返します．
        /// </summary>
        /// <returns></returns>
        public IOrderedEnumerable<int> GetExistingDiaryDays(bool IncludeEmptyDiary) =>
            this.GetExistingDiaryDates(IncludeEmptyDiary).Select(date => date.Day).OrderBy(day => day);
        /// <summary>
        /// 保存されているすべての日記の日付を返します．
        /// </summary>
        /// <param name="IncludeEmptyDiary"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public async Task<IOrderedEnumerable<DateTime>> GetExistingAllDiaryDatetimesAsync(bool IncludeEmptyDiary)
        {
            DateTime now = DateTime.Now;
            List<DateTime> ordered = new List<DateTime>();
            var yearMin = Convert.ToInt32(Application.Current.Resources["DiaryYearMin"]);
            var monthMin = Convert.ToInt32(Application.Current.Resources["DiaryMonthMin"]);
            var dayMin = Convert.ToInt32(Application.Current.Resources["DiaryDayMin"]);
            for (DateTime date = new DateTime(yearMin, monthMin, dayMin); date <= now; date = date.AddMonths(1))
            {
                if (date.Year == this.Year && date.Month == this.Month)
                    ordered.AddRange(this.GetExistingDiaryDates(IncludeEmptyDiary));
                else
                {
                    using (DiaryFileset diary = await OpenDiaryFilesetAsync(date, this.Key))
                    {
                        ordered.AddRange(diary.GetExistingDiaryDates(IncludeEmptyDiary));
                    }
                }
            }
            return ordered.OrderBy(date => date.Ticks);
        }
        /// <summary>
        /// 指定した名前のファイルを非同期的に読み込んで，その内容を返します．
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        public async Task<byte[]> ReadFileAsync(string filename) => await this.cryptoFileset.ReadAsync(filename);
        /// <summary>
        /// 指定したデータを，指定した名前と関連付けてこの日記に非同期的に保存します．そのファイル名が存在しない場合は新規作成されます．
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="data"></param>
        /// <param name="AllowOverwrite"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task WriteFileAsync(string filename, byte[] data, bool AllowOverwrite) => await this.cryptoFileset.WriteAsync(filename, data, AllowOverwrite);
        public async Task WriteImageAsync(string filename, WriteableBitmap bitmap, bool AllowOverWrite)
        {
            Guid guid;
            switch (Path.GetExtension(filename).ToLower())
            {
                case ".bmp": guid = BitmapEncoder.BmpEncoderId; break;
                case ".png": guid = BitmapEncoder.PngEncoderId; break;
                case ".gif": guid = BitmapEncoder.GifEncoderId; break;
                case ".jpg":
                case ".jpeg": guid = BitmapEncoder.JpegEncoderId; break;
                case ".tiff": guid = BitmapEncoder.TiffEncoderId; break;
                default: throw new ArgumentException($"{filename} is unsupported kind of file.");
            }
            using (var softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, bitmap.PixelWidth, bitmap.PixelHeight))
            {
                softwareBitmap.CopyFromBuffer(bitmap.PixelBuffer);
                using (var fileStream = new InMemoryRandomAccessStream())
                {
                    var encoder = await BitmapEncoder.CreateAsync(guid, fileStream);
                    encoder.SetSoftwareBitmap(softwareBitmap);
                    await encoder.FlushAsync();
                    fileStream.Seek(0);
                    byte[] data = new byte[fileStream.Size];
                    Windows.Storage.Streams.Buffer buffer = new Windows.Storage.Streams.Buffer((uint)fileStream.Size);
                    await fileStream.ReadAsync(buffer, (uint)fileStream.Size, InputStreamOptions.None);
                    await this.WriteFileAsync(filename, buffer.ToArray(), AllowOverWrite);
                }
            }
        }
        /// <summary>
        /// 指定したストレージをこの暗号化ファイルセットに保存し，保存した際のファイル名を返します．
        /// </summary>
        /// <param name="storage"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> WriteStorageAsync(IStorageFile storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            var filename = this.GetIdentifiableFilename(storage.Name);
            //ストリームを開いてストレージの中身を読み込み，ファイルセットにデータを追加する．
            using (var stream = (await storage.OpenReadAsync()).AsStreamForRead())
            {
                byte[] data = new byte[stream.Length];
                await stream.ReadAsync(data, 0, (int)stream.Length);
                await this.WriteFileAsync(filename, data, true);
            }
            return filename;
        }
        /// <summary>
        /// 指定した日付の日記を開き，その内容を返します．指定した日付の日記が存在しない場合，新規作成されます．
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> ReadDiaryAsync(DateTime date)
        {
            if (date.Year != this.Year || date.Month != this.Month)
            {
                this.cryptoFileset?.Dispose();
                this.cryptoFileset = null;
                this.cryptoFileset = await CryptoFileset.OpenOrCreateCryptoFilesetAsync(GetDiaryFilesetFolderName(date), GetDiaryFilesetName(date), this.Key, true);
                this.Date = date;
            }
            //指定された日付の日記がなければ新規作成
            var diaryName = GetDiaryName(date);
            if (!this.cryptoFileset.Exists(diaryName))
            {
                await this.WriteFileAsync(diaryName, new byte[0], false);
            }
            //操作対象の日付を更新し，内容を読み込んで返す．
            this.Day = date.Day;
            return encoding.GetString(await this.ReadFileAsync(diaryName));
        }
        /// <summary>
        /// 現在操作の対象となっている日記のテキストを更新します．
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="EncoderFallbackException"></exception>
        public async Task WriteDiaryAsync(string text)
        {
            await this.WriteFileAsync(GetDiaryName(this.Date), encoding.GetBytes(text), true);
        }
        /// <summary>
        /// 指定した名前のファイルをこの日記ファイルセットから削除します．
        /// </summary>
        /// <param name="filename"></param>
        public bool Remove(string filename) => this.cryptoFileset.Remove(filename);
        /// <summary>
        /// この日記ファイルセットの変更点をすべて保存します．
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public async Task FlushAsync() => await this.cryptoFileset.FlushAsync();
        /// <summary>
        /// この日記ファイルセットの暗号化および複合化に用いるキーを変更します．
        /// </summary>
        /// <param name="NewKey"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="IOException"></exception>
        public async Task ChangeKeyAsync(byte[] NewKey)
        {
            var now = DateTime.Now;
            var yearMin = Convert.ToInt32(Application.Current.Resources["DiaryYearMin"]);
            var monthMin = Convert.ToInt32(Application.Current.Resources["DiaryMonthMin"]);
            var dayMin = Convert.ToInt32(Application.Current.Resources["DiaryDayMin"]);
            for (DateTime date = new DateTime(yearMin, monthMin, dayMin); date <= now; date = date.AddMonths(1))
            {
                if (date.Year == this.Year && date.Month == this.Month)
                {
                    this.cryptoFileset.SetKey(NewKey);
                    await this.FlushAsync();
                }
                else
                {
                    using (var fileset = await OpenDiaryFilesetAsync(date, this.Key))
                    {
                        fileset.cryptoFileset.SetKey(NewKey);
                        await fileset.FlushAsync();
                    }
                }
            }
            this.Key = NewKey;
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.HasDisposed)
            {
                this.cryptoFileset?.Dispose();
                this.cryptoFileset = null;
            }
            base.Dispose(disposing);
        }
    }
    /// <summary>
    /// 日記を構成する要素の種類を列挙します．
    /// </summary>
    public enum DiaryEntryKind
    {
        ImageFlipView,
        //Media,
        Paragraph,
        HyperLink,
    }
    /// <summary>
    /// 日記要素を生成するためのデータを表現します．
    /// </summary>
    public class DiaryEntryInformation
    {
        public DiaryEntryKind Kind { get; set; }
        /// <summary>
        /// 日記要素に付与された属性コレクション．
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; }
        /// <summary>
        /// 日記要素に付与されたテキスト．
        /// </summary>
        public string Text { get; set; }
        public DiaryEntryInformation()
        {
            this.Attributes = new Dictionary<string, string>();
            this.Text = string.Empty;
        }
        /// <summary>
        /// 指定した属性キーに対応する属性値を返します．指定したキーが存在しない場合は第2引数の値が返されます．
        /// </summary>
        /// <param name="key"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        public string GetAttributeOr(string key, string DefaultValue = null)
        {
            return this.Attributes.ContainsKey(key) ? this.Attributes[key] : DefaultValue?.ToString();
        }
        public int GetAttributeInt32(string key, int DefaultValue = 0)
        {
            return int.TryParse(this.GetAttributeOr(key, DefaultValue.ToString()), out int value) ? value : DefaultValue;
        }
        public byte GetAttributeByte(string key, byte DefaultValue = 0)
        {
            return byte.TryParse(this.GetAttributeOr(key, DefaultValue.ToString()), out byte value) ? value : DefaultValue;
        }
        public bool GetAttributeBoolean(string key, bool DefaultValue = false)
        {
            return bool.TryParse(this.GetAttributeOr(key, DefaultValue.ToString()), out bool value) ? value : DefaultValue;
        }
    }
    /// <summary>
    /// 日記を構成する要素の基底クラスです．
    /// </summary>
    public abstract class DiaryEntry : Disposable
    {
        /// <summary>
        /// 属性名に使用不可能な文字．このフィールドは読み取り専用です．
        /// </summary>
        public static readonly char ProhibitedAttributeNameChar = '=';
        /// <summary>
        /// 属性値に使用不可能な文字．このフィールドは読み取り専用です．
        /// </summary>
        public static readonly char ProhibitedAttributeValueChar = '\"';
        private static readonly Regex diaryEntryMatchPattern;
        private static readonly Regex diaryAttributeMatchPattern;
        /// <summary>
        /// この要素の種類．このフィールドは読み取り専用です．
        /// </summary>
        public readonly DiaryEntryKind ElementKind;
        /// <summary>
        /// この要素の編集が許可されているか．このフィールドは読み取り専用です．
        /// </summary>
        public readonly bool AllowEdit;
        /// <summary>
        /// 派生クラスでオーバーライドされた場合，この要素の内容が変更されたかどうか取得します．
        /// </summary>
        public virtual bool HasEdited { get; protected set; }
        /// <summary>
        /// 派生クラスでオーバーライドされた場合，この要素に関連付けられた文字列を取得または設定します．
        /// </summary>
        public abstract string Text { get; set; }
        /// <summary>
        /// 派生クラスでオーバーライドされた場合，この要素に関連付けられたUI要素を取得します．
        /// </summary>
        public abstract FrameworkElement FrameworkElement { get; }
        /// <summary>
        /// 派生クラスでオーバーライドされた場合，この要素の内容が変更されたときに発生します．
        /// </summary>
        public abstract event EventHandler Edited;
        public event TappedEventHandler Tapped;
        static DiaryEntry()
        {
            StringBuilder entryPatternBuilder = new StringBuilder("<(");
            var entryKinds =
                from object entry in Enum.GetValues(typeof(DiaryEntryKind))
                select entry.ToString();
            entryPatternBuilder.AppendJoin('|', entryKinds);
            entryPatternBuilder.Append(") (\n|.)+?>(\n|.)*?</(");
            entryPatternBuilder.AppendJoin('|', entryKinds);
            entryPatternBuilder.Append(")>");
            diaryEntryMatchPattern = new Regex(entryPatternBuilder.ToString(), RegexOptions.Compiled);
            diaryAttributeMatchPattern = new Regex(" (.*?)" + ProhibitedAttributeNameChar + ProhibitedAttributeValueChar + "(.*?)" + ProhibitedAttributeValueChar, RegexOptions.Compiled);
        }
        /// <summary>
        /// 指定した種類の日記要素としてDiaryEntry型の新しいインスタンスを初期化します．
        /// </summary>
        /// <param name="ElementKind"></param>
        protected DiaryEntry(DiaryEntryKind ElementKind, bool allowEdit)
        {
            this.ElementKind = ElementKind;
            this.AllowEdit = true;
            this.HasEdited = false;
            this.AllowEdit = allowEdit;
        }
        protected virtual void OnTapped(object sender, TappedRoutedEventArgs e) => this.Tapped?.Invoke(this, e);
        /// <summary>
        /// 派生クラスでオーバーライドされた場合，この要素の属性コレクションを返します．
        /// </summary>
        /// <returns></returns>
        protected abstract Dictionary<string, string> GetAttributes();
        protected override void Dispose(bool disposing)
        {
            if (!this.HasDisposed) this.Tapped = null;
            base.Dispose(disposing);
        }
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder("<");
            builder.Append(this.ElementKind);
            builder.Append(' ');
            foreach (var attribute in this.GetAttributes())
            {
                builder.Append(' ');
                builder.Append(attribute.Key);
                builder.Append(ProhibitedAttributeNameChar);
                builder.Append(ProhibitedAttributeValueChar);
                builder.Append(attribute.Value);
                builder.Append(ProhibitedAttributeValueChar);
            }
            builder.Append(">");
            builder.Append((this.Text ?? string.Empty));
            builder.Append(("</" + this.ElementKind + ">\n"));
            return builder.ToString();
        }
        /// <summary>
        /// 指定した日付の日記を開き，その内容を返します．
        /// </summary>
        /// <param name="diary"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IEnumerable<DiaryEntryInformation> GetDiaryEntryInfos(string diaryText)
        {
            //パターンにマッチした文字列を順番に処理
            foreach (var match in diaryEntryMatchPattern.Matches(diaryText).Select(match => match.ToString()))
            {
                DiaryEntryInformation info = new DiaryEntryInformation();
                //日記要素の種類を取得
                var kindText = match.Substring(1, match.IndexOf(' ') - 1);
                info.Kind = (DiaryEntryKind)Enum.Parse(typeof(DiaryEntryKind), kindText);
                //属性情報とテキストを読み込む
                var entryNameRemoved = match.Substring(("<" + info.Kind).Length);
                var attributeMatchPattern = " (.*?)" + ProhibitedAttributeNameChar + ProhibitedAttributeValueChar + "(.*?)" + ProhibitedAttributeValueChar;
                //マッチした属性情報文字列を読み込んで，属性を追加
                foreach (var attributeText in diaryAttributeMatchPattern.Matches(entryNameRemoved).Select(match2 => match2.ToString()))
                {
                    var splited = attributeText.Trim(' ', '>').Split('=', 2);
                    info.Attributes.Add(splited.First(), splited.Last().Trim('\"'));
                }
                //テキスト部分を読み込む
                var textSource = Regex.Match(match, ">(\n|.)*</" + info.Kind + ">", RegexOptions.RightToLeft).ToString();
                info.Text = textSource.Substring(1, textSource.Length - 1 - ("</" + info.Kind + ">").Length).Trim('\n');
                yield return info;
            }
        }
        /// <summary>
        /// 指定した日付の日記を読み込み，テキスト部分をリストとして返します．
        /// </summary>
        /// <param name="diary"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IEnumerable<string> GetDiaryTexts(string diaryText)
        {
            var infos = GetDiaryEntryInfos(diaryText);
            foreach (var info in infos)
            {
                foreach (var attribute in info.Attributes) yield return attribute.Value;
                yield return info.Text;
            }
        }
        /// <summary>
        /// 指定した日記要素コレクションを，復元可能な文字列に変換して返します．
        /// </summary>
        /// <param name="entries"></param>
        /// <returns></returns>
        public static string DiaryEntriesToString(IEnumerable<DiaryEntry> entries)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var entry in entries) builder.Append(entry.ToString());
            return builder.ToString();
        }
    }
    public class DiaryParagraph : DiaryEntry
    {
        private bool isInitializing;
        private FrameworkElement textElement;
        public Color ForeColor
        {
            get
            {
                if (this.AllowEdit) return ((this.textElement as TextBox).Foreground as SolidColorBrush).Color;
                else return ((this.textElement as TextBlock).Foreground as SolidColorBrush).Color;
            }
            set
            {
                if (this.AllowEdit) (this.textElement as TextBox).Foreground = new SolidColorBrush(value);
                else (this.textElement as TextBlock).Foreground = new SolidColorBrush(value);
                this.HasEdited = true;
            }
        }
        public int FontSize
        {
            get
            {
                if (this.AllowEdit) return (int)(this.textElement as TextBox).FontSize;
                else return (int)(this.textElement as TextBlock).FontSize;
            }
            set
            {
                if (this.AllowEdit) (this.textElement as TextBox).FontSize = value;
                else (this.textElement as TextBlock).FontSize = value;
                this.HasEdited = true;
            }
        }
        public bool Bold
        {
            get
            {
                if (this.AllowEdit) return (this.textElement as TextBox).FontWeight.Weight == FontWeights.ExtraBold.Weight;
                else return (this.textElement as TextBlock).FontWeight.Weight == FontWeights.ExtraBold.Weight;
            }
            set
            {
                if (this.AllowEdit) (this.textElement as TextBox).FontWeight = value ? FontWeights.ExtraBold : FontWeights.Normal;
                else (this.textElement as TextBlock).FontWeight = value ? FontWeights.ExtraBold : FontWeights.Normal;
                this.HasEdited = true;
            }
        }
        public bool Italic
        {
            get
            {
                if (this.AllowEdit) return (this.textElement as TextBox).FontStyle == FontStyle.Italic;
                else return (this.textElement as TextBlock).FontStyle == FontStyle.Italic;
            }
            set
            {
                if (this.AllowEdit) (this.textElement as TextBox).FontStyle = value ? FontStyle.Italic : FontStyle.Normal;
                else (this.textElement as TextBlock).FontStyle = value ? FontStyle.Italic : FontStyle.Normal;
                this.HasEdited = true;
            }
        }
        public override bool HasEdited
        {
            get => base.HasEdited;
            protected set
            {
                //コンストラクタ以外の場所でプロパティを変更された場合は例外を投げる
                if (!this.AllowEdit && !this.isInitializing) throw new InvalidOperationException("このインスタンスは編集を許可されていません．");
                if (base.HasEdited = value) this.Edited?.Invoke(this, null);
            }
        }
        public override string Text
        {
            get
            {
                if (this.AllowEdit) return (this.textElement as TextBox).Text;
                else return (this.textElement as TextBlock).Text;
            }
            set
            {
                if (this.AllowEdit) (this.textElement as TextBox).Text = value;
                else (this.textElement as TextBlock).Text = value;
                this.HasEdited = true;
            }
        }
        public override FrameworkElement FrameworkElement => this.textElement;
        public override event EventHandler Edited;
        private DiaryParagraph(bool AllowEdit) : base(DiaryEntryKind.Paragraph, AllowEdit)
        {
            this.isInitializing = true;
            //UIを作成
            if (AllowEdit)
            {
                this.textElement = new TextBox()
                {
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                };
                (this.textElement as TextBox).TextChanged += delegate (object sender, TextChangedEventArgs e)
                {
                    this.HasEdited = true;
                    //日記解析でエラーを起こすような文字列を削除する
                    this.Text = this.Text.Replace("</" + this.ElementKind + ">", string.Empty);
                };
            }
            else
            {
                this.textElement = new TextBlock()
                {
                    TextWrapping = TextWrapping.Wrap,
                };
            }
            this.textElement.HorizontalAlignment = HorizontalAlignment.Stretch;
            if (AllowEdit) this.textElement.AddHandler(UIElement.TappedEvent, new TappedEventHandler(this.OnTapped), true);
            else this.textElement.AddHandler(UIElement.TappedEvent, new TappedEventHandler(this.OnTapped), true);
            this.HasEdited = false;
            this.isInitializing = false;
        }
        public static async Task<DiaryParagraph> CreateDiaryParagraphAsync(bool allowEdit, CoreDispatcher dispatcher)
        {
            DiaryParagraph paragraph = null;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => paragraph = new DiaryParagraph(allowEdit));
            return paragraph;
        }
        public static async Task<DiaryParagraph> CreateDiaryParagraphAsync(DiaryEntryInformation info, bool allowEdit, CoreDispatcher dispatcher)
        {
            DiaryParagraph paragraph = null;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                paragraph = new DiaryParagraph(allowEdit)
                {
                    isInitializing = true
                };
                var r = info.GetAttributeByte(nameof(paragraph.ForeColor.R));
                var g = info.GetAttributeByte(nameof(paragraph.ForeColor.G));
                var b = info.GetAttributeByte(nameof(paragraph.ForeColor.B));
                paragraph.ForeColor = Color.FromArgb(byte.MaxValue, r, g, b);
                paragraph.FontSize = info.GetAttributeInt32(nameof(paragraph.FontSize), Convert.ToInt32(Application.Current.Resources["ParagraphFontSizeDefault"]));
                paragraph.Bold = info.GetAttributeBoolean(nameof(paragraph.Bold));
                paragraph.Italic = info.GetAttributeBoolean(nameof(paragraph.Italic));
                paragraph.Text = info.Text;
                paragraph.isInitializing = false;
            });

            return paragraph;
        }
        protected override Dictionary<string, string> GetAttributes()
        {
            return new Dictionary<string, string>
            {
                { nameof(this.ForeColor.R), this.ForeColor.R.ToString() },
                { nameof(this.ForeColor.G), this.ForeColor.G.ToString() },
                { nameof(this.ForeColor.B), this.ForeColor.B.ToString() },
                { nameof(this.FontSize), this.FontSize.ToString() },
                { nameof(this.Bold), this.Bold.ToString() },
                { nameof(this.Italic), this.Italic.ToString() }
            };
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.HasDisposed)
            {
                this.Edited = null;
                this.textElement = null;
            }
            base.Dispose(disposing);
        }
    }
    public class DiaryImageFlipView : DiaryEntry
    {
        private static readonly string widthAttribute = "width";
        private static readonly string heightAttribute = "height";
        private static readonly string imageExplanationAttribute = "ex";
        private ImageFlipView imageFlipView;
        private bool isInitializing;
        public int Width
        {
            get => (int)this.imageFlipView.Width;
            set
            {
                this.imageFlipView.Width = value;
                this.HasEdited = true;
            }
        }
        public int Height
        {
            get => (int)this.imageFlipView.Height;
            set
            {
                this.imageFlipView.Height = value;
                this.HasEdited = true;
            }
        }
        public string SelectedImageExplanation
        {
            get => this.imageFlipView.SelectedImageExplanation;
            set => this.imageFlipView.SelectedImageExplanation = value;
        }
        public override bool HasEdited
        {
            get => base.HasEdited;
            protected set
            {
                if (!this.AllowEdit && !isInitializing) throw new InvalidOperationException();
                if (base.HasEdited = value) this.Edited?.Invoke(this, null);
            }
        }
        public override string Text { get => string.Empty; set => throw new NotImplementedException(); }
        public override FrameworkElement FrameworkElement => this.imageFlipView;
        public override event EventHandler Edited;
        public event EventHandler<string> SelectedImageExplanationChanged;
        private DiaryImageFlipView(bool AllowEdit) : base(DiaryEntryKind.ImageFlipView, AllowEdit)
        {
            //this.isConstructing = true;
            this.isInitializing = true;
            //
            this.imageFlipView = new ImageFlipView();
            this.Width = Convert.ToInt32(Application.Current.Resources["ImageFlipViewSizeDefault"]);
            this.Height = Convert.ToInt32(Application.Current.Resources["ImageFlipViewSizeDefault"]);
            this.imageFlipView.AddHandler(UIElement.TappedEvent, new TappedEventHandler(this.OnTapped), true);
            this.imageFlipView.SelectedImageExplanationChanged += this.OnSelectedImageExplanatioChanged;
            //
            this.HasEdited = false;
            this.isInitializing = false;
            //this.isConstructing = false;
        }
        public static async Task<DiaryImageFlipView> CreateDiaryImageFlipViewAsync(bool allowEdit, CoreDispatcher dispatcher)
        {
            DiaryImageFlipView flipView = null;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => flipView = new DiaryImageFlipView(allowEdit));
            return flipView;
        }
        public static async Task<DiaryImageFlipView> CreateDiaryImageFlipViewAsync(DiaryFileset diary, DiaryEntryInformation info, bool allowEdit, CoreDispatcher dispatcher)
        {
            DiaryImageFlipView flipView = null;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => flipView = new DiaryImageFlipView(allowEdit));
            flipView.isInitializing = true;
            //サイズを読み込む．読み込みに失敗した場合は規定のサイズにする
            flipView.Width = info?.GetAttributeInt32(widthAttribute, Convert.ToInt32(Application.Current.Resources["ImageFlipViewSizeDefault"])) ?? Convert.ToInt32(Application.Current.Resources["ImageFlipViewSizeDefault"]);
            flipView.Height = info?.GetAttributeInt32(heightAttribute, Convert.ToInt32(Application.Current.Resources["ImageFlipViewSizeDefault"])) ?? Convert.ToInt32(Application.Current.Resources["ImageFlipViewSizeDefault"]);
            //画像ファイル名を示す属性を取得し，番号順に並び変える
            Regex indexRegex = new Regex(@"^(\d)+$");
            var orderedImageFilenameAttributes =
                from attribute in info?.Attributes
                where indexRegex.IsMatch(attribute.Key)
                orderby int.Parse(indexRegex.Match(attribute.Key).Value)
                select attribute;
            //画像ファイルの説明を示す属性を取得
            Regex explanationRegex = new Regex($@"^{imageExplanationAttribute}(\d)+");
            var orderedImageExplanationAttributes =
                from attribute in info?.Attributes
                where explanationRegex.IsMatch(attribute.Key)
                orderby int.Parse(explanationRegex.Match(attribute.Key).Value.Substring(imageExplanationAttribute.Length))
                select attribute;
            //画像ファイル名と画像説明のペアを生成
            var imageInformationDictionary =
                orderedImageFilenameAttributes.Zip(orderedImageExplanationAttributes, (index, explanation) => new KeyValuePair<string, string>(index.Value, explanation.Value));
            //各属性ごとに読み込む
            foreach (var imageInformation in imageInformationDictionary)
            {
                var (name, explanation) = imageInformation;
                var bytes = await diary.ReadFileAsync(name);
                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    BitmapImage bitmap = new BitmapImage();
                    await stream.WriteAsync(bytes.AsBuffer());
                    stream.Seek(0);
                    bitmap.SetSource(stream);
                    flipView.imageFlipView.AddImage(name, explanation, bitmap);
                }
            }
            //
            flipView.HasEdited = false;
            flipView.isInitializing = false;
            return flipView;
        }
        public async Task AddImageFileAsync(IStorageFile storage, string savedName = null)
        {
            var imagename = savedName ?? Path.GetFileName(storage.Name);
            using (var stream = await storage.OpenStreamForReadAsync())
            {
                using (var istream = stream.AsRandomAccessStream())
                {
                    BitmapImage image = new BitmapImage();
                    await image.SetSourceAsync(istream);
                    this.imageFlipView.AddImage(imagename, string.Empty, image);
                }
            }
            this.HasEdited = true;
        }
        public void AddImage(string ImageName, ImageSource source)
        {
            this.imageFlipView.AddImage(ImageName, string.Empty, source);
            this.HasEdited = true;
        }
        public string RemoveSelectedImage()
        {
            this.HasEdited = true;
            return this.imageFlipView.RemoveSelectedImage();
        }
        public IEnumerable<string> RemoveAllImages()
        {
            this.HasEdited = true;
            return this.imageFlipView.RemoveAllImages();
        }
        private void OnSelectedImageExplanatioChanged(object sender, string e)
        {
            this.SelectedImageExplanationChanged?.Invoke(this, e);
        }
        protected override Dictionary<string, string> GetAttributes()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>
            {
                { widthAttribute, this.Width.ToString() },
                { heightAttribute, this.Height.ToString() }
            };
            for (int i = 0; i < this.imageFlipView.ImageNames.Count(); i++)
            {
                dictionary.Add(i.ToString(), this.imageFlipView.ImageNames.ElementAt(i));
                dictionary.Add(imageExplanationAttribute + i, this.imageFlipView.ImageExplanations.ElementAt(i));
            }
            return dictionary;
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.HasDisposed)
            {
                this.Edited = null;
                this.imageFlipView.SelectedImageExplanationChanged -= this.OnSelectedImageExplanatioChanged;
                this.imageFlipView.RemoveAllImages();
                this.imageFlipView = null;
            }
            base.Dispose(disposing);
        }
    }
#if false
    public class DiaryMediaPlayer : DiaryEntry
    {
        private static readonly string filenameAttribute = "source";
        private static readonly string contentTypeAttribute = "contentType";
        public string SourceFilename;
        private string contentType;
        private IRandomAccessStream mediaSourceRandomAccessStream;
        private MediaPlayerElement mediaPlayer;
        public override string Text { get => string.Empty; set => throw new NotImplementedException(); }
        public override FrameworkElement FrameworkElement => this.mediaPlayer;
        public override event EventHandler Edited;
        private DiaryMediaPlayer(bool allowEdit) : base(DiaryEntryKind.Media, allowEdit)
        {
            this.mediaPlayer = new MediaPlayerElement()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                AreTransportControlsEnabled = true,
            };
            this.mediaPlayer.AddHandler(UIElement.TappedEvent, new TappedEventHandler(this.OnTapped), true);
        }
        public static async Task<DiaryMediaPlayer> CreateDiaryMediaPlayerAsync(DiaryFileset diary, IStorageFile mediaStorageFile, CoreDispatcher dispatcher)
        {
            if (!DiaryFileset.SupportedMediaExtenions.Contains(mediaStorageFile.FileType))
                throw new NotSupportedException($"{mediaStorageFile.Name} is a kind of not-supported media.");
            DiaryMediaPlayer mediaPlayer = null;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => mediaPlayer = new DiaryMediaPlayer(true));
            //ファイル名を取得
            mediaPlayer.SourceFilename = diary.GetIdentifiableFilename(mediaStorageFile.Name);
            mediaPlayer.contentType = mediaStorageFile.ContentType;
            //日記ファイルセットにデータを保存
            mediaPlayer.mediaSourceRandomAccessStream = await mediaStorageFile.OpenReadAsync();
            byte[] data = new byte[mediaPlayer.mediaSourceRandomAccessStream.Size];
            await mediaPlayer.mediaSourceRandomAccessStream.ReadAsync(data.AsBuffer(), (uint)mediaPlayer.mediaSourceRandomAccessStream.Size, InputStreamOptions.None);
            await diary.WriteFileAsync(mediaPlayer.SourceFilename, data, true);
            mediaPlayer.mediaSourceRandomAccessStream.Seek(0);
            mediaPlayer.mediaPlayer.Source = MediaSource.CreateFromStream(mediaPlayer.mediaSourceRandomAccessStream, mediaPlayer.contentType);
            return mediaPlayer;
        }
        public static async Task<DiaryMediaPlayer> CreateDiaryMediaPlayerAsync(DiaryFileset diary, DiaryEntryInformation info, bool allowEdit, CoreDispatcher dispatcher)
        {
            DiaryMediaPlayer mediaPlayer = null;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => mediaPlayer = new DiaryMediaPlayer(allowEdit));
            //名前とコンテンツの種類を取得
            mediaPlayer.SourceFilename = info.GetAttributeOr(filenameAttribute);
            mediaPlayer.contentType = info.GetAttributeOr(contentTypeAttribute);
            //日記ファイルセットから音楽データを読み込む
            var data = await diary.ReadFileAsync(mediaPlayer.SourceFilename);
            mediaPlayer.mediaSourceRandomAccessStream = new InMemoryRandomAccessStream();
            await mediaPlayer.mediaSourceRandomAccessStream.WriteAsync(data.AsBuffer());
            mediaPlayer.mediaSourceRandomAccessStream.Seek(0);
            mediaPlayer.mediaPlayer.Source = MediaSource.CreateFromStream(mediaPlayer.mediaSourceRandomAccessStream, mediaPlayer.contentType);
            return mediaPlayer;
        }
        protected override Dictionary<string, string> GetAttributes()
        {
            return new Dictionary<string, string>
            {
                { contentTypeAttribute, this.contentType },
                { filenameAttribute, this.SourceFilename }
            };
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.HasDisposed)
            {
                this.Edited = null;
                this.mediaPlayer?.MediaPlayer?.Pause();
                this.mediaPlayer?.MediaPlayer?.Dispose();
                this.mediaPlayer = null;
                this.mediaSourceRandomAccessStream?.Dispose();
                this.mediaSourceRandomAccessStream = null;
            }
            base.Dispose(disposing);
        }
    }
#endif
    public class DiaryHyperLink : DiaryEntry
    {
        private static readonly string uriAttribute = "uri";
        private bool isInitializing;
        private FrameworkElement frameworkElement;
        private Uri _navigateUri;
        private string _explanationText;
        public Uri NavigateUri
        {
            get => this._navigateUri;
            set
            {
                this._navigateUri = value;
                if (this.frameworkElement is TextBlock)
                {
                    (this.frameworkElement as TextBlock).Text = this.Text + $" ({value?.ToString() ?? "クリックしてリンク先を設定"})";
                }
                else
                {
                    (this.frameworkElement as HyperlinkButton).NavigateUri = value;
                    (this.frameworkElement as HyperlinkButton).Content = this.Text + $" ({value?.ToString() ?? "クリックしてリンク先を設定"})";
                }
                this.HasEdited = true;
            }
        }
        public override bool HasEdited
        {
            get => base.HasEdited;
            protected set
            {
                //コンストラクタ以外の場所でプロパティを変更された場合は例外を投げる
                if (!this.AllowEdit && !this.isInitializing) throw new InvalidOperationException("このインスタンスは編集を許可されていません．");
                if (base.HasEdited = value) this.Edited?.Invoke(this, null);
            }
        }
        public override string Text
        {
            get => this._explanationText;
            set
            {
                this._explanationText = value;
                if (this.frameworkElement is TextBlock) (this.frameworkElement as TextBlock).Text = value + $" ({this.NavigateUri?.ToString() ?? "クリックしてリンク先を設定"})";
                else (this.frameworkElement as HyperlinkButton).Content = value + $" ({this.NavigateUri?.ToString() ?? "クリックしてリンク先を設定"})";
                this.HasEdited = true;
            }
        }
        public override FrameworkElement FrameworkElement => this.frameworkElement;
        public override event EventHandler Edited;
        private DiaryHyperLink(bool allowEdit) : base(DiaryEntryKind.HyperLink, allowEdit)
        {
            this.isInitializing = true;
            if (allowEdit) this.frameworkElement = new TextBlock();
            else this.frameworkElement = new HyperlinkButton();
            this.frameworkElement.AddHandler(UIElement.TappedEvent, new TappedEventHandler(this.OnTapped), true);
            this.Text = "リンク先の説明";
            this.isInitializing = false;
        }
        public static async Task<DiaryHyperLink> CreateDiaryHyperLinkAsync(Uri navigateUri, bool allowEdit, CoreDispatcher dispatcher)
        {
            DiaryHyperLink hyperLink = null;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => hyperLink = new DiaryHyperLink(allowEdit));
            hyperLink.isInitializing = true;
            hyperLink.NavigateUri = navigateUri;
            hyperLink.isInitializing = false;
            return hyperLink;
        }
        public static async Task<DiaryHyperLink> CreateDiaryHyperLinkAsync(DiaryEntryInformation info, bool allowEdit, CoreDispatcher dispatcher)
        {
            DiaryHyperLink hyperLink = null;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => hyperLink = new DiaryHyperLink(allowEdit));
            hyperLink.isInitializing = true;
            var uriString = info.GetAttributeOr(uriAttribute, string.Empty);
            if (Uri.IsWellFormedUriString(uriString, UriKind.Absolute)) hyperLink.NavigateUri = new Uri(uriString);
            hyperLink.Text = info.Text;
            hyperLink.isInitializing = false;
            return hyperLink;
        }
        protected override Dictionary<string, string> GetAttributes()
        {
            return new Dictionary<string, string>
            {
                { uriAttribute, this.NavigateUri?.ToString() ?? string.Empty }
            };
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.HasDisposed)
            {
                this.Edited = null;
                this.frameworkElement = null;
            }
            base.Dispose(disposing);
        }
    }
    public class KeywordSurrounding
    {
        public DateTime DateTime { get; }
        public string DateString => this.DateTime.ToShortDateString();
        /// <summary>
        /// キーワード直前の文字列．
        /// </summary>
        public string Former { get; }
        /// <summary>
        /// キーワード．
        /// </summary>
        public string Keyword { get; }
        /// <summary>
        /// キーワード直後の文字列．
        /// </summary>
        public string Latter { get; }
        public KeywordSurrounding() { }
        /// <summary>
        /// 指定した文字列リストから，検索結果一覧で表示するデータを生成します．
        /// </summary>
        /// <param name="paragraphs">検索対象となる文字列リスト．</param>
        /// <param name="keywords">検索キーワード．</param>
        /// <param name="surroundingStringLength">キーワードの周辺何文字を検索結果一覧で表示するか．</param>
        public KeywordSurrounding(IEnumerable<string> paragraphs, IEnumerable<string> keywords, int surroundingStringLength, DateTime date)
        {
            //いずれかのキーワードを含む段落の中で，もっとも先頭に近いものを選択．改行文字はすべて半角スペースに置き換える
            var targetParagraph = paragraphs.Where(paragraph => keywords.Any(keyword => paragraph.Contains(keyword))).First().Replace('\r', '\n').Replace('\n', ' ');
            //キーワードの中で，最も段落先頭に近い位置にあるものを選択
            var targetKeyword =
                (from keyword in keywords
                 where targetParagraph.Contains(keyword)
                 orderby targetParagraph.IndexOf(keyword)
                 select keyword).First();
            //段落中の文字列におけるキーワードの先頭位置と終端位置を取得
            var keywordBeginIndex = targetParagraph.IndexOf(targetKeyword);
            var keywordEndIndex = keywordBeginIndex + targetKeyword.Length - 1;
            //キーワード前後の文字列とキーワードを記録
            if (keywordBeginIndex == 0)
            {
                this.Former = "";
            }
            else if (keywordBeginIndex < surroundingStringLength)
            {
                this.Former = targetParagraph.Substring(0, keywordBeginIndex);
            }
            else
            {
                this.Former = targetParagraph.Substring(keywordBeginIndex - surroundingStringLength, surroundingStringLength);
                if (keywordBeginIndex - surroundingStringLength > 0) this.Former = "..." + this.Former;
            }
            this.Keyword = targetKeyword;
            if (keywordEndIndex == targetParagraph.Length)
            {
                this.Latter = "";
            }
            else if (targetParagraph.Length - keywordEndIndex < surroundingStringLength + 1)
            {
                this.Latter = targetParagraph.Substring(keywordEndIndex + 1);
            }
            else
            {
                this.Latter = targetParagraph.Substring(keywordEndIndex + 1, surroundingStringLength);
                if (keywordEndIndex + 1 + surroundingStringLength < targetParagraph.Length) this.Latter += "...";
            }
            this.DateTime = date;
        }
    }
    /// <summary>
    /// コントロールキーを含むショートカットキーが入力されたときに発生するイベントで渡される値．
    /// </summary>
    public class ShortcutKeyDownEventAges : EventArgs
    {
        public readonly bool IsShiftKeyDown;
        public readonly VirtualKey PrimaryKey;
        public ShortcutKeyDownEventAges(bool IsShiftKeyDown, VirtualKey PrimaryKey)
        {
            this.IsShiftKeyDown = IsShiftKeyDown;
            this.PrimaryKey = PrimaryKey;
        }
        public override string ToString()
        {
            return (this.IsShiftKeyDown ? "Shift+" : string.Empty) + "Ctrl+" + PrimaryKey;
        }
    }
    /// <summary>
    /// ショートカットキーの入力を監視します．
    /// </summary>
    public class ShortcutMonitor
    {
        /// <summary>
        /// 現在押されているキーのリスト．
        /// </summary>
        private List<VirtualKey> keyDown;
        public bool this[VirtualKey key]
        {
            get => this.keyDown.Contains(key);
            private set
            {
                if (this[key] && !value) this.keyDown.Remove(key);
                else if (!this[key] && value) this.keyDown.Add(key);
            }
        }
        /// <summary>
        /// コントロールキーを含むショートカットキーが入力されたときに発生します．
        /// </summary>
        public event EventHandler<ShortcutKeyDownEventAges> ShortcutKeyDown;
        public ShortcutMonitor()
        {
            this.keyDown = new List<VirtualKey>();
        }
        public void OnKeyDown(VirtualKey key)
        {
            this[key] = true;
            if (key != VirtualKey.Control && key != VirtualKey.Shift && this[VirtualKey.Control])
                this.ShortcutKeyDown?.Invoke(this, new ShortcutKeyDownEventAges(this[VirtualKey.Shift], key));
        }
        public void OnKeyUp(VirtualKey key) => this[key] = false;
        public void Clear()
        {
            this.keyDown.Clear();
        }
    }
    public static class CycleColorCollection
    {
        private static readonly IList<Color> colors = new List<Color> { Colors.Black, Colors.DarkRed, Colors.DarkGreen, Colors.DarkBlue, Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Pink, Colors.Purple, Colors.Brown, Colors.Gray };
        public static Color Next(Color current)
        {
            var index = colors.IndexOf(current);
            if (index == -1) return colors[0];
            return colors[index == colors.Count - 1 ? 0 : index + 1];
        }
        public static Color Previous(Color current)
        {
            var index = colors.IndexOf(current);
            if (index == -1) return colors[0];
            return colors[index == 0 ? colors.Count - 1 : index - 1];
        }
    }
}
