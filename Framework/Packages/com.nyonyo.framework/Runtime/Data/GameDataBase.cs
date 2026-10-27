using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Nyonyo.Framework.Assets;
using UnityEngine;

namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// 모든 데이터 테이블을 불러오는 서비스의 공통 부분입니다. 데이터 가져오기가 게임 폴더에 상속 클래스(기본 이름 <c>GameData</c>)를 만듭니다.
    /// </summary>
    /// <remarks>
    /// 서비스로 등록하면 게임 시작 때 모든 테이블을 비동기로 불러옵니다(<see cref="IAsyncInitializable"/>).
    /// 다 불러온 뒤 <see cref="OnLoaded"/>가 호출되므로, 테이블끼리 참조를 연결하는 코드는 게임 쪽 partial 클래스에서 재정의하세요.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.Register(new GameData(assets));
    /// var sword = GameData.Instance.Item.Get(1001);
    /// </code>
    /// </example>
    public abstract class GameDataBase : IAsyncInitializable
    {
        private readonly IAssetLoader _loader;

        protected GameDataBase(IAssetLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader), "IAssetLoader가 없습니다. AssetLoader를 먼저 만들어 넘기세요.");
        }

        public bool IsLoaded { get; private set; }

        /// <summary>시트 순서대로 담긴 모든 테이블입니다.</summary>
        public abstract IReadOnlyList<DataTableBase> Tables { get; }

        protected abstract LoadType LoadType { get; }

        /// <summary>데이터 파일 경로 형식입니다. {0}에 테이블 이름이 들어갑니다.</summary>
        protected abstract string PathFormat { get; }

        protected abstract byte[] EncryptionKey { get; }

        public Awaitable InitializeAsync(CancellationToken cancellationToken) => LoadAllAsync(cancellationToken);

        /// <summary>모든 테이블을 비동기로 불러옵니다. 서비스로 등록했다면 직접 부를 필요가 없습니다.</summary>
        /// <exception cref="InvalidDataException">데이터 파일이 손상되었거나 코드와 맞지 않는 경우</exception>
        public async Awaitable LoadAllAsync(CancellationToken cancellationToken = default)
        {
            var crypto = new GameDataCrypto(EncryptionKey);
            foreach (var table in Tables)
            {
                var path = string.Format(PathFormat, table.Name);
                var asset = await _loader.LoadAsync<TextAsset>(path, LoadType, cancellationToken);
                LoadTable(crypto, table, path, asset);
            }

            FinishLoading();
        }

        /// <summary>모든 테이블을 바로 불러옵니다. WebGL에서 Addressables를 쓰면 동기 로드가 안 되므로 <see cref="LoadAllAsync"/>를 쓰세요.</summary>
        public void LoadAll()
        {
            var crypto = new GameDataCrypto(EncryptionKey);
            foreach (var table in Tables)
            {
                var path = string.Format(PathFormat, table.Name);
                LoadTable(crypto, table, path, _loader.Load<TextAsset>(path, LoadType));
            }

            FinishLoading();
        }

        /// <summary>
        /// 모든 테이블을 다 불러온 뒤 호출됩니다. 게임 쪽 partial 클래스에서 재정의해 테이블끼리 참조를 연결하세요.
        /// </summary>
        protected virtual void OnLoaded()
        {
        }

        /// <summary>암호화된 파일 내용으로 테이블 하나를 불러옵니다. 에셋 로더를 거치지 않으므로 테스트에서 씁니다.</summary>
        internal void LoadTable(DataTableBase table, byte[] file) => LoadTable(new GameDataCrypto(EncryptionKey), table, file);

        private void LoadTable(GameDataCrypto crypto, DataTableBase table, string path, TextAsset asset)
        {
            try
            {
                LoadTable(crypto, table, asset.bytes);
            }
            finally
            {
                // 행 객체로 옮겼으니 원본 파일은 메모리에서 내립니다.
                _loader.Release(path, LoadType);
            }
        }

        private void LoadTable(GameDataCrypto crypto, DataTableBase table, byte[] file)
        {
            try
            {
                table.Load(crypto.Decrypt(file));
            }
            catch (InvalidDataException e)
            {
                throw new InvalidDataException($"[Nyonyo Framework] 게임 데이터 '{table.Name}'를 읽지 못했습니다. {e.Message}", e);
            }
        }

        private void FinishLoading()
        {
            IsLoaded = true;
            OnLoaded();
        }
    }
}
