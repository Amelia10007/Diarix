using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;
using System.Security.Cryptography;
using System.ComponentModel;
using Windows.Storage.Streams;

namespace Diarix
{
    public static class ClassEx
    {
        /// <summary>
        /// 指定したフォルダ直下にこのファイルをコピーします．
        /// </summary>
        /// <param name="storageFile"></param>
        /// <param name="targetFolder"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task CopyAsync(this IStorageFile storageFile, IStorageFolder targetFolder, CreationCollisionOption option = CreationCollisionOption.FailIfExists)
        {
            //ファイルを作成
            var createdFile = await targetFolder.CreateFileAsync(storageFile.Name, option);
            //中身をコピー
            using (var writer = await createdFile.OpenStreamForWriteAsync())
            {
                using (var reader = await storageFile.OpenStreamForReadAsync())
                {
                    if (reader.Length > int.MaxValue)
                        throw new InvalidOperationException("ファイルサイズが大きすぎます．");
                    byte[] buffer = new byte[reader.Length];
                    await reader.ReadAsync(buffer, 0, buffer.Length);
                    await writer.WriteAsync(buffer, 0, buffer.Length);
                    await writer.FlushAsync();
                }
            }
        }
        /// <summary>
        /// 指定したフォルダ直下にこのフォルダとその中に存在するすべてのフォルダ・ファイルをコピーします．
        /// </summary>
        /// <param name="storageFolder"></param>
        /// <param name="targetFolder"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task CopyAsync(this IStorageFolder storageFolder, IStorageFolder targetFolder, CreationCollisionOption option = CreationCollisionOption.FailIfExists)
        {
            //フォルダを作成
            var createdFolder = await targetFolder.CreateFolderAsync(storageFolder.Name, option);
            //子フォルダのコピー
            foreach (var childFolder in await storageFolder.GetFoldersAsync())
            {
                //子フォルダを作成
                var createdChildFolder = await createdFolder.CreateFolderAsync(childFolder.Name, option);
                //再帰
                await childFolder.CopyAsync(createdChildFolder, option);
            }
            //ファイルのコピー
            foreach (var file in await storageFolder.GetFilesAsync())
            {
                await file.CopyAsync(createdFolder);
            }
        }
        /// <summary>
        /// 指定した比較方法をもとに，指定した要素がリストのどの位置に存在するか返します．
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="item"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static int IndexOf<T>(this IEnumerable<T> collection, T item, IEqualityComparer<T> comparer)
        {
            if (!collection.Any()) return -1;
            for (int i = 0; i < collection.Count(); i++)
            {
                if (comparer.Equals(collection.ElementAt(i), item)) return i;
            }
            return -1;
        }
    }
    /// <summary>
    /// アンマネージリソース開放機構を提供する基底クラスです．
    /// </summary>
    public class Disposable : IDisposable
    {
        /// <summary>
        /// このオブジェクトが既に破棄されたか取得します．
        /// </summary>
        protected bool HasDisposed
        {
            get;
            private set;
        }
        /// <summary>
        /// Disposable型の新しいインスタンスを初期化します．
        /// </summary>
        public Disposable()
        {
            this.HasDisposed = false;
        }
        /// <summary>
        /// このオブジェクトが保持しているリソースを全て開放します．
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// このオブジェクトが保持しているリソースを開放します．
        /// </summary>
        /// <param name="disposing">マネージリソースを開放するか．</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.HasDisposed) return;
            if (disposing)
            {
                //dispose managed resource
            }
            //dispose umanaged resource
            //
            this.HasDisposed = true;
        }
        /// <summary>
        /// デストラクタ．
        /// </summary>
        ~Disposable()
        {
            this.Dispose(false);
        }
    }
    public static class ApplicationStorage
    {
        private static readonly StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
        public static async Task<bool> Exists(string filename) => await Exists(new string[0], filename);
        public static async Task<bool> Exists(string foldername, string filename) =>
            await Exists(new[] { foldername }, filename);
        public static async Task<bool> Exists(IEnumerable<string> foldernames, string filename)
        {
            var targetFolder = storageFolder;
            //子フォルダへ移動
            foreach (var foldername in foldernames ?? Enumerable.Empty<string>())
            {
                if ((targetFolder = await targetFolder.TryGetItemAsync(foldername) as StorageFolder) == null) return false;
            }
            //ファイルの存在確認
            return await targetFolder.TryGetItemAsync(filename) != null;
        }
        public static async Task<StorageFile> GetOrCreateFileAsync(string filename) =>
            await GetOrCreateFileAsync(new string[0], filename);
        public static async Task<StorageFile> GetOrCreateFileAsync(string foldername, string filename) =>
            await GetOrCreateFileAsync(new[] { foldername }, filename);
        public static async Task<StorageFile> GetOrCreateFileAsync(IEnumerable<string> foldernames, string filename)
        {
            var targetFolder = storageFolder;
            //子フォルダへ移動
            foreach (var foldername in foldernames ?? Enumerable.Empty<string>())
            {
                var child = await targetFolder.TryGetItemAsync(foldername) as StorageFolder;
                if (child == null) targetFolder = await targetFolder.CreateFolderAsync(foldername);
                else targetFolder = child;
            }
            var targetFile = await targetFolder.TryGetItemAsync(filename) as StorageFile;
            if (targetFile == null) targetFile = await targetFolder.CreateFileAsync(filename);
            return targetFile;
        }
        /// <summary>
        /// ローカルフォルダに存在する全てのファイルの名前と異なるファイル名を返します．
        /// </summary>
        /// <param name="desiredFilename">ファイル名．</param>
        /// <returns></returns>
        public static async Task<string> GetIdentifiableFilenameAsync(string desiredFilename) =>
            await GetIdentifiableFilenameAsync(new string[0], desiredFilename, false);
        /// <summary>
        /// 指定したフォルダに存在する全てのファイルの名前と異なるファイル名を返します．
        /// </summary>
        /// <param name="foldername">操作対象のフォルダ名．</param>
        /// <param name="desiredFilename">ファイル名．</param>
        /// <param name="createFolderOption">trueに設定し，フォルダが存在しない場合はフォルダが新規作成されます．</param>
        /// <returns></returns>
        public static async Task<string> GetIdentifiableFilenameAsync(string foldername, string desiredFilename, bool createFolderOption) =>
            await GetIdentifiableFilenameAsync(new[] { foldername }, desiredFilename, createFolderOption);
        /// <summary>
        /// 指定したフォルダに存在する全てのファイルの名前と異なるファイル名を返します．
        /// </summary>
        /// <param name="foldernames">操作対象のフォルダ名．</param>
        /// <param name="desiredFilename">ファイル名．</param>
        /// <param name="createFolderOption">trueに設定し，フォルダが存在しない場合はフォルダが新規作成されます．</param>
        /// <returns></returns>
        public static async Task<string> GetIdentifiableFilenameAsync(IEnumerable<string> foldernames, string desiredFilename, bool createFolderOption)
        {
            var targetFolder = storageFolder;
            //子フォルダへ移動
            foreach (var foldername in foldernames ?? Enumerable.Empty<string>())
            {
                var child = await targetFolder.TryGetItemAsync(foldername) as StorageFolder;
                if (child == null)
                {
                    if (createFolderOption) targetFolder = await targetFolder.CreateFolderAsync(foldername);
                    else return null;
                }
                else
                {
                    targetFolder = child;
                }
            }
            //指定した名前のファイルが存在しなければ希望通りのファイル名を返す
            if (await targetFolder.TryGetItemAsync(desiredFilename) == null) return desiredFilename;
            //拡張子の直前に数字をつけて，他と被らないファイル名を作る
            int i = 1;
            var name = Path.GetFileNameWithoutExtension(desiredFilename) + $"({i})" + Path.GetExtension(desiredFilename);
            while (await targetFolder.TryGetItemAsync(name) != null)
            {
                i++;
                name = Path.GetFileNameWithoutExtension(desiredFilename) + $"({i})" + Path.GetExtension(desiredFilename);
            }
            //
            return name;
        }
    }
    /// <summary>
    /// 暗号化されたファイルセットに対する基本的な操作をサポートします．
    /// </summary>
    public sealed class CryptoFileset : Disposable
    {
        /// <summary>
        /// ファイルセットに保存可能な最大ファイル数．このフィールドは読み取り専用です．
        /// </summary>
        private static readonly int fileNumMax = int.MaxValue;
        /// <summary>
        /// ファイルセットに保存可能な最大ファイルサイズ．このフィールドは読み取り専用です．
        /// </summary>
        private static readonly int fileSizeMax = int.MaxValue;
        private static readonly long fileSizeSumMax = long.MaxValue;
        private class CodedFile
        {
            /// <summary>
            /// このファイルのファイル名をバイトシーケンスにした場合のデータサイズ
            /// </summary>
            public int NameSize;
            public string Name;
            public int Size;
            public byte[] Data;
            public bool OverWritten;
            public int OriginalSize;
            public bool RequaredRemove;
            public CodedFile()
            {
            }
            public CodedFile(string name, byte[] data)
            {
                this.NameSize = Encoding.Unicode.GetBytes(name).Length;
                this.Name = name;
                this.Size = data.Length * sizeof(byte);
                this.Data = data;
                this.OverWritten = false;
                this.RequaredRemove = false;
            }
        }
        /// <summary>
        /// 現在管理しているファイルセットが存在するフォルダ．
        /// </summary>
        private IEnumerable<string> targetFoldernames;
        /// <summary>
        /// 現在管理しているファイルセットの名前
        /// </summary>
        private string targetFilename;
        /// <summary>
        /// ファイルセット中に保存，または新たに追加されたファイル群のリスト．
        /// </summary>
        private List<CodedFile> files;
        /// <summary>
        /// ファイルセットの読み込みおよび書き込みに使用するストリーム．
        /// </summary>
        private Stream stream;
        /// <summary>
        /// ファイルセットの暗号化・復号に使用するキー．
        /// </summary>
        public byte[] Key { get; private set; }
        /// <summary>
        /// ファイルセットを開いたときに使用したキー．
        /// </summary>
        private byte[] originalKey;
        /// <summary>
        /// 指定したファイル名に対応するCodedDirectoryStream.CodedFileを返します．
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private CodedFile this[string filename]
        {
            get
            {
                return this.files.Find(file => file.Name == filename);
            }
            set
            {
                var target = this.files.Find(file => file.Name == filename);
                target.OverWritten = value.OverWritten;
                target.Size = value.Size;
                target.Data = value.Data;
            }
        }
        private CryptoFileset()
        {
            this.files = new List<CodedFile>();
        }
        /// <summary>
        /// 指定したパスの暗号化ファイルセットを開きます．
        /// </summary>
        /// <param name="filename">暗号化ファイルセットの名前．</param>
        /// <param name="key">暗号化ファイルセットに適用するキー．</param>
        /// <param name="createOption">trueに設定した場合，ファイルが存在しない場合は新規作成されます．</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        public static async Task<CryptoFileset> OpenOrCreateCryptoFilesetAsync(string filename, byte[] key, bool createOption) =>
            await OpenOrCreateCryptoFilesetAsync(new string[0], filename, key, createOption);
        /// <summary>
        /// 指定したパスの暗号化ファイルセットを開きます．
        /// </summary>
        /// <param name="foldernams">暗号化ファイルセットが存在するフォルダ．</param>
        /// <param name="filename">暗号化ファイルセットの名前．</param>
        /// <param name="key">暗号化ファイルセットに適用するキー．</param>
        /// <param name="createOption">trueに設定した場合，ファイルやフォルダが存在しない場合は新規作成されます．</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        public static async Task<CryptoFileset> OpenOrCreateCryptoFilesetAsync(string foldername, string filename, byte[] key, bool createOption) =>
            await OpenOrCreateCryptoFilesetAsync(new[] { foldername }, filename, key, createOption);
        /// <summary>
        /// 指定したパスの暗号化ファイルセットを開きます．
        /// </summary>
        /// <param name="foldernames">暗号化ファイルセットが存在するフォルダ．</param>
        /// <param name="filename">暗号化ファイルセットの名前．</param>
        /// <param name="key">暗号化ファイルセットに適用するキー．</param>
        /// <param name="createOption">trueに設定した場合，ファイルやフォルダが存在しない場合は新規作成されます．</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        public static async Task<CryptoFileset> OpenOrCreateCryptoFilesetAsync(IEnumerable<string> foldernames, string filename, byte[] key, bool createOption)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (key.Length == 0) throw new ArgumentException($"{nameof(key)} must have a element at least.");
            var alreadyExisted = await ApplicationStorage.Exists(foldernames, filename);
            if (!alreadyExisted)
            {
                if (createOption) await ApplicationStorage.GetOrCreateFileAsync(foldernames, filename);
                else throw new FileNotFoundException("The specified file does not exist.");
            }
            var storage = await ApplicationStorage.GetOrCreateFileAsync(foldernames, filename);
            CryptoFileset cryptoFileset = new CryptoFileset()
            {
                targetFoldernames = foldernames,
                targetFilename = filename,
                Key = new byte[key.Length],
                originalKey = new byte[key.Length],
            };
            if (alreadyExisted)
            {
                try
                {
                    cryptoFileset.stream = await storage.OpenStreamForReadAsync();
                    //中身が空ならまだセーブしてない状態なので，読み込まずに終了
                    if (cryptoFileset.stream.Length == 0) return cryptoFileset;
                    //
                    var fileCount = cryptoFileset.ReadInt32();
                    while (cryptoFileset.files.Count < fileCount)
                    {
                        CodedFile file = new CodedFile()
                        {
                            //ファイル名のサイズを読み込む
                            NameSize = cryptoFileset.ReadInt32(),
                            //ファイルサイズを読み込む
                            OriginalSize = cryptoFileset.ReadInt32()
                        };
                        file.Size = file.OriginalSize;
                        //ファイル名を取得
                        file.Name = Encoding.Unicode.GetString(cryptoFileset.ReadBytes(file.NameSize), 0, file.NameSize);
                        //ファイルの中身は読み飛ばす。シークして次のファイルデータの先頭位置へ
                        cryptoFileset.stream.Seek(file.OriginalSize, SeekOrigin.Current);
                        //読み込んだ内容を追加
                        cryptoFileset.files.Add(file);
                    }
                }
                catch
                {
                    throw new IOException("Cannot read the file.");
                }
            }
            return cryptoFileset;
        }
        /// <summary>
        /// パスワードに基づいて平文を暗号文に変換して返します．
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private byte[] Encrypt(byte[] plain)
        {
            if (plain == null) throw new ArgumentNullException(nameof(plain));
            byte[] value = new byte[plain.Length];
            for (int i = 0; i < plain.Length; i++)
            {
                value[i] = (byte)(~plain[i] ^ this.originalKey[i % this.originalKey.Length]);
            }
            return value;
        }
        /// <summary>
        /// パスワードに基づいて暗号文を平文に変換して返します．
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private byte[] Decrypt(byte[] code)
        {
            if (code == null) throw new ArgumentNullException(nameof(code));
            return this.Encrypt(code);
        }
        /// <summary>
        /// 暗号化されたバイトシーケンスをストリームの現在位置から読み込んで，平文に復元して返します．
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        private byte[] ReadBytes(int count)
        {
            byte[] array = new byte[count];
            this.stream.Read(array, 0, count);
            return this.Decrypt(array);
        }
        /// <summary>
        /// 暗号化されたバイトシーケンスをストリームの現在位置から読み込んで，平文に復元して返します．
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        private async Task<byte[]> ReadBytesAsync(int count)
        {
            byte[] array = new byte[count];
            await this.stream.ReadAsync(array, 0, count);
            return this.Decrypt(array);
        }
        /// <summary>
        ///暗号化された整数値をストリームの現在位置から読み込んで，平文に復元して返します．
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        private int ReadInt32()
        {
            return BitConverter.ToInt32(this.ReadBytes(sizeof(int)), 0);
        }
        /// <summary>
        ///暗号化された整数値をストリームの現在位置から読み込んで，平文に復元して返します．
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        private async Task<int> ReadInt32Async()
        {
            return BitConverter.ToInt32(await this.ReadBytesAsync(sizeof(int)), 0);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task WriteBytesAsync(byte[] data)
        {
            var codedData = this.Encrypt(data);
            await this.stream.WriteAsync(codedData, 0, codedData.Length);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task WriteInt32Async(int data)
        {
            await this.WriteBytesAsync(BitConverter.GetBytes(data));
        }
        /// <summary>
        /// 指定した名前のファイルがこの暗号化ディレクトリに存在するか返します．
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool Exists(string filename)
        {
            return this.files.Exists(file => file.Name == filename && !file.RequaredRemove);
        }
        /// <summary>
        /// 指定したファイルのサイズを返します．
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>ファイルサイズ．ファイルが存在しない場合は-1．</returns>
        public long Sizeof(string filename)
        {
            return this.Exists(filename) ? this[filename].Size : -1;
        }
        /// <summary>
        /// 指定した名前のファイルを読み込んで，その内容を返します．
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        public async Task<byte[]> ReadAsync(string filename)
        {
            if (!this.Exists(filename) || this[filename].RequaredRemove) throw new FileNotFoundException("ファイル:" + filename + "は存在しません．");
            //読み込もうとしてるファイルの中身をすでに読み込み済みなら、そのまま返す
            if (this[filename].Data != null) return this[filename].Data;
            //ファイルの中身が保存されている先頭位置を求める
            //読み込もうとしてるファイルのインデックスを取得
            var index = this.files.IndexOf(this[filename]);
            //ファイル数を示すデータのサイズ
            long offset = sizeof(int);
            //このファイルよりも前方に保存されているファイルの名前や中身のサイズ
            if (index > 0) offset += this.files.Take(index).Sum(file => (long)(sizeof(int) + sizeof(int) + file.NameSize + file.OriginalSize));
            //このファイルのファイルの名前のサイズを示す変数のサイズ，中身サイズを示す変数のサイズ，ファイル名サイズ
            offset += sizeof(int) + sizeof(int) + this[filename].NameSize;
            //求めた位置へシーク
            this.stream.Seek(offset, SeekOrigin.Begin);
            //ファイルサイズ分だけ読み込んで返す
            return this[filename].Data = await this.ReadBytesAsync(this[filename].OriginalSize);
        }
        /// <summary>
        /// 指定した名前のファイルを読み込んで，その内容を格納したメモリストリームを返します．
        /// </summary>
        /// <param name="filename"></param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        /// <returns></returns>
        public async Task<MemoryStream> ReadStreamAsync(string filename)
        {
            MemoryStream memoryStream = new MemoryStream(await this.ReadAsync(filename));
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
        /// <summary>
        /// 指定したデータを，指定したファイル名と関連付けしてこのファイルセットに保存します．そのファイルが存在しない場合は新規作成されます．
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="data"></param>
        /// <param name="allowOverwrite">ファイルが既に存在する場合にデータを上書きするか．</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">書き込もうとしているデータのサイズが大きすぎる場合にスローされます．</exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException">既にファイルセットに保存されているファイル数が上限に達している場合にスローされます．</exception>
        public async Task WriteAsync(string filename, byte[] data, bool allowOverwrite)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.LongCount() > fileSizeMax) throw new ArgumentException("data is too long.");
            if (this.files.LongCount() > fileNumMax) throw new InvalidOperationException("files are too many.");
            if (sizeof(int) +
                this.files.Where(file => !file.RequaredRemove).Sum(file => (decimal)(sizeof(int) + sizeof(int) + file.NameSize + file.Size))
                + sizeof(int) + sizeof(int) + Encoding.Unicode.GetByteCount(filename) + data.Length > fileSizeSumMax)
                throw new InvalidOperationException("filesize has already amounted to the maximum.");
            //既に同名ファイルが存在し，上書きが許可されている場合は内容を上書きし，削除フラグや上書きフラグを設定
            if (this.Exists(filename))
            {
                if (allowOverwrite)
                {
                    this[filename].RequaredRemove = false;
                    this[filename].OverWritten = true;
                    this[filename].Data = new byte[data.Length];
                    data.CopyTo(this[filename].Data, 0);
                    this[filename].Size = data.Length * sizeof(byte);
                }
            }
            else
            {
                this.files.Add(new CodedFile(filename, data) { OverWritten = true, });
            }
        }
        /// <summary>
        /// 指定した名前のファイルをこの暗号化ディレクトリから削除します．
        /// </summary>
        /// <param name="filename"></param>
        public bool Remove(string filename)
        {
            if (!this.Exists(filename)) return false;
            this[filename].RequaredRemove = true;
            return true;
        }
        /// <summary>
        /// このファイルセットの変更点を全てファイルに保存します．
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public async Task FlushAsync()
        {
            //上書きされたファイルや削除フラグが立ったファイルsが存在しなければ何もしないで終了
            if (!this.files.Any(file => file.OverWritten || file.RequaredRemove)) return;
            //削除フラグが立ったファイルをリストから削除
            this.files.RemoveAll(file => file.RequaredRemove);
            //まだ読み込まれていないファイルを読み込む．同時に複数のファイルが読み込まれるのを防ぐためにWait()する
            foreach (var file in this.files)
            {
                if (file.Data == null) await this.ReadAsync(file.Name);
            }
            //キーを更新
            this.originalKey = new byte[this.Key.Length];
            this.Key.CopyTo(this.originalKey, 0);
            //
            this.stream?.Dispose();
            //書き込みモードでファイルを開き直す
            this.stream = await (await ApplicationStorage.GetOrCreateFileAsync(this.targetFoldernames, this.targetFilename)).OpenStreamForWriteAsync();
            this.stream.SetLength(0);
            //ファイル数を書き込む
            await this.WriteInt32Async(this.files.Count);
            //各ファイルの内容を書き込む
            foreach (var file in files)
            {
                //ファイル名サイズ
                await this.WriteInt32Async(file.NameSize);
                //ファイルサイズ
                await this.WriteInt32Async(file.Size);
                //ファイル名
                await this.WriteBytesAsync(Encoding.Unicode.GetBytes(file.Name));
                //ファイルの中身
                await this.WriteBytesAsync(file.Data);
            }
            //保存
            await this.stream.FlushAsync();
            this.stream.Dispose();
            //読み込みモードでファイルを開き直す
            this.stream = await (await ApplicationStorage.GetOrCreateFileAsync(this.targetFoldernames, this.targetFilename)).OpenStreamForReadAsync();
            //読み込んだファイルの中身と，上書き保存フラグをリセット(メモリ使用量を減らす)
            foreach (var file in files)
            {
                file.OverWritten = false;
                file.OriginalSize = file.Size;
                file.Data = null;
            }
        }
        /// <summary>
        /// ファイルセットの暗号化・復号に用いるキーを変更します．
        /// </summary>
        /// <param name="NewKey"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void SetKey(byte[] NewKey)
        {
            if (NewKey == null) throw new ArgumentNullException("NewKey");
            if (NewKey.Length == 0) throw new ArgumentException("キーは長さ1以上に設定して下さい．");
            this.Key = new byte[NewKey.Length];
            NewKey.CopyTo(this.Key, 0);
        }
        /// <summary>
        /// このオブジェクトが保有しているリソースを全て開放します．
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!this.HasDisposed)
            {
                this.stream?.Dispose();
                this.stream = null;
                this.targetFoldernames = null;
                this.files = null;
            }
            base.Dispose(disposing);
        }
    }
    /// <summary>
    /// パスワード文字列からハッシュを生成します．
    /// </summary>
    public class PasswordEncoder
    {
        private static readonly int saltSize = 64;
        private static readonly int hashSize = 64;
        private static readonly int iteration = 1000;
        /// <summary>
        /// 生成されたソルト．このフィールドは読み取り専用です．
        /// </summary>
        public readonly byte[] Salt;
        /// <summary>
        /// 生成されたハッシュ．このフィールドは読み取り専用です．
        /// </summary>
        public readonly byte[] Hash;
        private static byte[] CreateSalt()
        {
            var bytes = new byte[saltSize];
            using (RandomNumberGenerator provider = new RNGCryptoServiceProvider())
            {
                provider.GetBytes(bytes);
            }
            return bytes;
        }
        /// <summary>
        /// 指定したパスワード文字列とソルトからハッシュを生成します．
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public static byte[] CreateHash(string password, byte[] salt)
        {
            using (Rfc2898DeriveBytes deriver = new Rfc2898DeriveBytes(password, salt, iteration))
            {
                return deriver.GetBytes(hashSize);
            }
        }
        /// <summary>
        /// 指定したパスワード文字列を用いて，PasswordEncoder型の新しいインスタンスを初期化します．
        /// </summary>
        /// <param name="password"></param>
        /// <exception cref="ArgumentNullException">passwordがnullです．</exception>
        /// <exception cref="CryptographicException">暗号操作中にエラーが発生しました．</exception>
        public PasswordEncoder(string password)
        {
            this.Salt = CreateSalt();
            this.Hash = CreateHash(password, this.Salt);
        }
    }
}
