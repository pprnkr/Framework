namespace Nyonyo.Framework.Ads
{
    /// <summary>
    /// 광고 노출로 생긴 수익 정보입니다. 분석 도구(Firebase 등)에 광고 수익을 보낼 때 씁니다.
    /// </summary>
    public readonly struct AdRevenue
    {
        public readonly AdFormat Format;
        public readonly string AdUnitId;

        /// <summary>수익을 100만 배 한 정수입니다. 예: 0.005달러면 5000입니다.</summary>
        public readonly long ValueMicros;

        /// <summary>ISO 4217 통화 코드입니다(예: USD).</summary>
        public readonly string CurrencyCode;

        /// <summary>값의 정확도입니다. 광고 네트워크가 주는 문자열 그대로입니다(예: Precise, Estimated).</summary>
        public readonly string Precision;

        public AdRevenue(AdFormat format, string adUnitId, long valueMicros, string currencyCode, string precision)
        {
            Format = format;
            AdUnitId = adUnitId;
            ValueMicros = valueMicros;
            CurrencyCode = currencyCode;
            Precision = precision;
        }

        /// <summary>통화 단위의 수익입니다(<see cref="ValueMicros"/> / 1,000,000).</summary>
        public double Value => ValueMicros / 1_000_000d;
    }
}
