﻿
namespace ATAS.Indicators.Technical
{
    #region INCLUDES

    using System;
    using System.Media;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Drawing;
    using System.Net; 
    using ATAS.Indicators;
    using ATAS.Indicators.Drawing;
    using OFT.Attributes.Editors;
    using Newtonsoft.Json.Linq;
    using OFT.Rendering.Context;
    using OFT.Rendering.Tools;
    using static ATAS.Indicators.Technical.SampleProperties;

    using Color = System.Drawing.Color; 
    using MColor = System.Windows.Media.Color;
    using MColors = System.Windows.Media.Colors; 
    using Pen = System.Drawing.Pen;
    using String = System.String;
    using System.Globalization;
    using OFT.Rendering.Settings;
    using Newtonsoft.Json;
    using System.Text;
    using Utils.Common;

    #endregion

    [DisplayName("TraderOracle Buy/Sell")]
    public class BuySell : Indicator
    {
        private const String sVersion = "3.1";
        private int iTouched = 0;
        private bool bVolImbFinished = false;
        private bool bIgnoreBadWicks = true;

        #region PRIVATE FIELDS

        private PenSettings defibPen = new PenSettings
        {
            Color = DefaultColors.Red.Convert(),
            Width = 1,
            LineDashStyle = LineDashStyle.Dot
        };

        private struct bars
        {
            public String s;
            public int bar;
            public bool top;
        }

        private RenderStringFormat _format = new()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        private List<bars> lsBar = new List<bars>();
        private List<string> lsH = new List<string>();
        private List<string> lsM = new List<string>();
        private bool bBigArrowUp = false;
        private static readonly HttpClient client = new HttpClient();
        private readonly PaintbarsDataSeries _paintBars = new("Paint bars");

        private String _highS = "1st Hour High";
        private String _lowS = "1st Hour Low";
        private String _highL = "London High";
        private String _lowL = "London Low";
        private String sWavDir = @"C:\Program Files (x86)\ATAS Platform\Sounds";
        private int _highBar;
        private int _lowBar;
        private decimal _highest = 0;
        private decimal _lowest = 0;
        private int _highBarL;
        private int _lowBarL;
        private decimal _highestL = 0;
        private decimal _lowestL = 0;
        private int _lastBar = -1;
        private int iBigTrades = 25000;
        private int iShavedRatio = 1;
        private int iFutureSound = 0;
        private bool _lastBarCounted;
        private Color lastColor = Color.White;
        private Color colorEngulfg = Color.FromArgb(255, 0, 61, 29);
        private Color colorEngulfr = Color.FromArgb(255, 87, 3, 3);
        private MColor colorShavedg = MColor.FromRgb(154, 204, 255);
        private MColor colorShavedr = MColor.FromRgb(247, 150, 70);
        private String lastEvil = "";
        private bool bShowUp = true;
        private bool bShowDown = true;
        private bool bShowFirstHour = false;
        private bool bShowLondon = false;

        // Default TRUE
        private bool bShowEngBB = true;
        private bool bShowTramp = true;          // SHOW
        private bool bShowNews = true;
        private bool bUseFisher = true;          // USE
        private bool bUseWaddah = true;
        private bool bUseT3 = false;
        private bool bUsePSAR = true;
        private bool bShowMACDPSARArrow = false;
        private bool bShowRegularBuySell = true;
        private bool bVolumeImbalances = true;
        private bool bShowSquare = false;
        private bool bShowShaved = true;

        private bool bKAMAWick = true;
        private bool bNewsProcessed = false;     // USE
        private bool bUseSuperTrend = false;
        private bool bUseSqueeze = false;
        private bool bUseMACD = true;
        private bool bUseKAMA = false;
        private bool bUseMyEMA = false;
        private bool bUseAO = false;
        private bool bUseHMA = true;
        private bool bShowDojiCity = true;

        private bool bShow921 = false;
        private bool bShowSqueeze = false;
        private bool bShowRevPattern = false;
        private bool bShowTripleSupertrend = false;
        private bool bShowCloud = false;
        private bool bAdvanced = false;
        private bool bShowStar = false;
        private bool bShowEvil = true;
        private bool bShowClusters = false;
        private bool bShowLines = false;
        private bool bShowWaddahLine = false;

        private int iMinDelta = 0;
        private int iMinDeltaPercent = 0;
        private int iMinADX = 11;
        private int iMyEMAPeriod = 21;
        private int iKAMAPeriod = 9;
        private int iOffset = 9;
        private int iFontSize = 10;
        private int iNewsFont = 10;
        private int iWaddaSensitivity = 150;
        private int iMACDSensitivity = 70;
        private int CandleColoring = 0;

        private int iMinBid = 9;
        private int iMinAsk = 9;
        private int iClusterRatio = 3;

        #endregion

        #region SETTINGS

        [Display(GroupName = "Buy/Sell Indicators", Name = "Show buy/sell dots")]
        public bool ShowRegularBuySell { get => bShowRegularBuySell; set { bShowRegularBuySell = value; RecalculateValues(); } }
        [Display(GroupName = "Buy/Sell Indicators", Name = "Show MACD/PSAR arrow")]
        public bool ShowBigArrow { get => bShowMACDPSARArrow; set { bShowMACDPSARArrow = value; RecalculateValues(); } }
        [Display(GroupName = "Buy/Sell Indicators", Name = "Show reversal square")]
        public bool ShowSquare{ get => bShowSquare; set { bShowSquare = value; RecalculateValues(); } }

        [Display(GroupName = "Buy/Sell Indicators", Name = "Use Alert Sounds")]
        public bool UseAlerts { get; set; }

        // ========================================================================
        // =======================    FILTER INDICATORS    ========================
        // ========================================================================

        [Display(GroupName = "Buy/Sell Filters", Name = "Waddah Explosion", Description = "The Waddah Explosion must be the correct color, and have a value")]
        public bool Use_Waddah_Explosion { get => bUseWaddah; set { bUseWaddah = value; RecalculateValues(); } }
        [Display(GroupName = "Buy/Sell Filters", Name = "Awesome Oscillator", Description = "AO is positive or negative")]
        public bool Use_Awesome { get => bUseAO; set { bUseAO = value; RecalculateValues(); } }
        [Display(GroupName = "Buy/Sell Filters", Name = "Parabolic SAR", Description = "The PSAR must be signaling a buy/sell signal same as the arrow")]
        public bool Use_PSAR { get => bUsePSAR; set { bUsePSAR = value; RecalculateValues(); } }
        [Display(GroupName = "Buy/Sell Filters", Name = "Squeeze Momentum", Description = "The squeeze must be the correct color")]
        public bool Use_Squeeze_Momentum { get => bUseSqueeze; set { bUseSqueeze = value; RecalculateValues(); } }
        [Display(GroupName = "Buy/Sell Filters", Name = "MACD", Description = "Standard 12/26/9 MACD crossing in the correct direction")]
        public bool Use_MACD { get => bUseMACD; set { bUseMACD = value; RecalculateValues(); } }
        [Display(GroupName = "Buy/Sell Filters", Name = "Hull Moving Avg", Description = "Price must align to the HMA trend")]
        public bool Use_HMA { get => bUseHMA; set { bUseHMA = value; RecalculateValues(); } }
        [Display(GroupName = "Buy/Sell Filters", Name = "SuperTrend", Description = "Price must align to the current SuperTrend trend")]
        public bool Use_SuperTrend { get => bUseSuperTrend; set { bUseSuperTrend = value; RecalculateValues(); } }
        [Display(GroupName = "Buy/Sell Filters", Name = "T3", Description = "Price must cross the T3")]
        public bool Use_T3 { get => bUseT3; set { bUseT3 = value; RecalculateValues(); } }
        [Display(GroupName = "Buy/Sell Filters", Name = "Fisher Transform", Description = "Fisher Transform must cross to the correct direction")]
        public bool Use_Fisher_Transform { get => bUseFisher; set { bUseFisher = value; RecalculateValues(); } }

        [Display(GroupName = "Buy/Sell Filters", Name = "Ignore bad candlesticks", Description = "If candlestick pattern doesn't fit the trade signal, don't show")]
        public bool IgnoreBadWicks { get => bIgnoreBadWicks; set { bIgnoreBadWicks = value; RecalculateValues(); } }

        [Display(GroupName = "Buy/Sell Filters", Name = "Minimum ADX", Description = "Minimum ADX value before showing buy/sell")]
        [Range(0, 100)]
        public int Min_ADX { get => iMinADX; set { if (value < 0) return; iMinADX = value; RecalculateValues(); } }

        [Display(GroupName = "Cluster Signals", Name = "Show Cluster Lines")]
        public bool ShowClusters { get => bShowClusters; set { bShowClusters = value; RecalculateValues(); } }
        [Display(GroupName = "Cluster Signals", Name = "Cluster Volume Ratio")]
        [Range(0, 9000)]
        public int ClusterRatio { get => iClusterRatio; set { if (value < 0) return; iClusterRatio = value; RecalculateValues(); } }
        [Display(GroupName = "Cluster Signals", Name = "Minimum Bid")]
        [Range(0, 9000)]
        public int MinBid { get => iMinBid; set { if (value < 0) return; iMinBid = value; RecalculateValues(); } }
        [Display(GroupName = "Cluster Signals", Name = "Minimum Ask")]
        [Range(0, 9000)]
        public int MinAsk { get => iMinAsk; set { if (value < 0) return; iMinAsk = value; RecalculateValues(); } }

        [Display(GroupName = "Custom MA Filter", Name = "Use Custom EMA", Description = "Price crosses your own EMA period")]
        public bool Use_Custom_EMA { get => bUseMyEMA; set { bUseMyEMA = value; RecalculateValues(); } }
        [Display(GroupName = "Custom MA Filter", Name = "Custom EMA Period", Description = "Price crosses your own EMA period")]
        [Range(1, 1000)]
        public int Custom_EMA_Period
        { get => iMyEMAPeriod; set { if (value < 1) return; iMyEMAPeriod = _myEMA.Period = value; RecalculateValues(); } }

        [Display(GroupName = "Custom MA Filter", Name = "Use KAMA", Description = "Price crosses KAMA")]
        public bool Use_KAMA { get => bUseKAMA; set { bUseKAMA = value; RecalculateValues(); } }
        [Display(GroupName = "Custom MA Filter", Name = "KAMA Period", Description = "Price crosses KAMA")]
        [Range(1, 1000)]
        public int Custom_KAMA_Period { get => iKAMAPeriod; set { if (value < 1) return; iKAMAPeriod = _kama9.EfficiencyRatioPeriod = value; RecalculateValues(); } }

        private class candleColor : Collection<Entity>
        {
            public candleColor()
                : base(new[]
                {
                    new Entity { Value = 1, Name = "None" },
                    new Entity { Value = 2, Name = "Waddah Explosion" },
                    new Entity { Value = 3, Name = "Squeeze" },
                    new Entity { Value = 4, Name = "Delta" },
                    new Entity { Value = 5, Name = "MACD" }
                })
            { }
        }
        [Display(Name = "Candle Color", GroupName = "Colored Candles")]
        [ComboBoxEditor(typeof(candleColor), DisplayMember = nameof(Entity.Name), ValueMember = nameof(Entity.Value))]
        public int canColor { get => CandleColoring; set { if (value < 0) return; CandleColoring = value; RecalculateValues(); } }

        [Display(GroupName = "Colored Candles", Name = "Color BB engulfing candles")]
        public bool ShowEngBB { get => bShowEngBB; set { bShowEngBB = value; RecalculateValues(); } }

        [Display(GroupName = "Colored Candles", Name = "Color shaved candles")]
        public bool ShowShaved { get => bShowShaved; set { bShowShaved = value; RecalculateValues(); } }

        [Display(GroupName = "Colored Candles", Name = "Shave candle ratio")]
        public int ShavedRatio { get => iShavedRatio; set { iShavedRatio = value; RecalculateValues(); } }

        [Display(GroupName = "Colored Candles", Name = "Show Reversal Patterns")]
        public bool ShowRevPattern { get => bShowRevPattern; set { bShowRevPattern = value; RecalculateValues(); } }
        [Display(GroupName = "Colored Candles", Name = "Show Advanced Ideas")]
        public bool ShowBrooks { get => bAdvanced; set { bAdvanced = value; RecalculateValues(); } }
        [Display(GroupName = "Colored Candles", Name = "Waddah Sensitivity")]
        [Range(0, 9000)]
        public int WaddaSensitivity { get => iWaddaSensitivity; set { if (value < 0) return; iWaddaSensitivity = value; RecalculateValues(); } }
        [Display(GroupName = "Colored Candles", Name = "MACD Sensitivity")]
        [Range(0, 9000)]
        public int MACDSensitivity { get => iMACDSensitivity; set { if (value < 0) return; iMACDSensitivity = value; RecalculateValues(); } }

        [Display(GroupName = "Colored Candles", Name = "Engulfing GREEN Candle Color")]
        public Color colEngulf { get => colorEngulfg; set { colorEngulfg = value; RecalculateValues(); } }
        [Display(GroupName = "Colored Candles", Name = "Engulfing RED Candle Color")]
        public Color colEngulfr { get => colorEngulfr; set { colorEngulfr = value; RecalculateValues(); } }

        [Display(GroupName = "Colored Candles", Name = "Shaved GREEN Candle Color")]
        public MColor colShaved { get => colorShavedg; set { colorShavedg = value; RecalculateValues(); } }
        [Display(GroupName = "Colored Candles", Name = "Shaved RED Candle Color")]
        public MColor colrShaved { get => colorShavedr; set { colorShavedr = value; RecalculateValues(); } }

        [Display(GroupName = "Extras", Name = "Delta intensity threshold")]
        public int BigTrades { get => iBigTrades; set { iBigTrades = value; RecalculateValues(); } }

        [Display(GroupName = "Extras", Name = "Show doji cities")]
        public bool ShowDojiCity { get => bShowDojiCity; set { bShowDojiCity = value; RecalculateValues(); } }

        [Display(GroupName = "Extras", Name = "Show kama wicks")]
        public bool KAMAWick { get => bKAMAWick; set { bKAMAWick = value; RecalculateValues(); } }

        [Display(GroupName = "Extras", Name = "WAV Sound Directory")]
        public String WavDir { get => sWavDir; set { sWavDir = value; RecalculateValues(); } }

        [Display(GroupName = "Extras", Name = "Show Kama/EMA 200/VWAP lines")]
        public bool ShowLines { get => bShowLines; set { bShowLines = value; RecalculateValues(); } }

        [Display(GroupName = "Extras", Name = "Show Triple Supertrend")]
        public bool ShowTripleSupertrend { get => bShowTripleSupertrend; set { bShowTripleSupertrend = value; RecalculateValues(); } }
        [Display(GroupName = "Extras", Name = "Show 9/21 EMA Cross")]
        public bool Show_9_21_EMA_Cross { get => bShow921; set { bShow921 = value; RecalculateValues(); } }
        [Display(GroupName = "Extras", Name = "Show Squeeze Relaxer")]
        public bool Show_Squeeze_Relax { get => bShowSqueeze; set { bShowSqueeze = value; RecalculateValues(); } }
        [Display(GroupName = "Extras", Name = "Show Volume Imbalances", Description = "Show gaps between two candles, indicating market strength")]
        public bool Use_VolumeImbalances { get => bVolumeImbalances; set { bVolumeImbalances = value; RecalculateValues(); } }
        [Display(GroupName = "Extras", Name = "Show Nebula Cloud", Description = "Show cloud containing KAMA 9 and 21")]
        public bool Use_Cloud { get => bShowCloud; set { bShowCloud = value; RecalculateValues(); } }

        [Display(GroupName = "Extras", Name = "Show Waddah Lines", Description = "Show large lines on screen when Waddah is long/short")]
        public bool ShowWaddahLine { get => bShowWaddahLine; set { bShowWaddahLine = value; RecalculateValues(); } }

        [Display(GroupName = "Extras", Name = "Show Trampoline", Description = "Trampoline is the ultimate reversal indicator")]
        public bool Use_Tramp { get => bShowTramp; set { bShowTramp = value; RecalculateValues(); } }

        [Display(GroupName = "Extras", Name = "Show Evil Times", Description = "Market timing from FighterOfEvil, on Discord")]
        public bool ShowEvil { get => bShowEvil; set { bShowEvil = value; RecalculateValues(); } } 

        [Display(GroupName = "Extras", Name = "Show Star Times", Description = "Market timing from Star, on Discord")]
        public bool ShowStar { get => bShowStar; set { bShowStar = value; RecalculateValues(); } }

        [Display(GroupName = "Extras", Name = "Show First Hour Lines", Description = "Show lines from first hour of NY Session")]
        public bool ShowFirstHour { get => bShowFirstHour; set { bShowFirstHour = value; RecalculateValues(); } }

        [Display(GroupName = "Extras", Name = "Show London Session Lines", Description = "Show lines from London session")]
        public bool ShowLondon { get => bShowLondon; set { bShowLondon = value; RecalculateValues(); } }

        [Display(GroupName = "High Impact News", Name = "Show today's news")]
        public bool Show_News { get => bShowNews; set { bShowNews = value; RecalculateValues(); } }

        [Display(GroupName = "High Impact News", Name = "News font")]
        [Range(1, 900)]
        public int NewsFont
        { get => iNewsFont; set { iNewsFont = value; RecalculateValues(); } }

        private decimal VolSec(IndicatorCandle c) { return c.Volume / Convert.ToDecimal((c.LastTime - c.Time).TotalSeconds); }

        #endregion

        #region CONSTRUCTOR

        public BuySell() :
            base(true)
        {
            EnableCustomDrawing = true;
            DenyToChangePanel = true;
            SubscribeToDrawingEvents(DrawingLayouts.Historical);

            DataSeries[0] = _posSeries;
            DataSeries.Add(_negSeries);
            DataSeries.Add(_negWhite);
            DataSeries.Add(_posWhite);
            DataSeries.Add(_negRev);
            DataSeries.Add(_posRev);
            DataSeries.Add(_negBBounce);
            DataSeries.Add(_posBBounce);
            DataSeries.Add(_nine21);
            DataSeries.Add(_squeezie);
            DataSeries.Add(_paintBars);

            DataSeries.Add(_dnTrend);
            DataSeries.Add(_upTrend);
            DataSeries.Add(_upCloud);
            DataSeries.Add(_dnCloud);

            DataSeries.Add(_lineVWAP);
            DataSeries.Add(_lineEMA200);
            DataSeries.Add(_lineKAMA);

            Add(_ao);
            Add(_ft);
            Add(_sq);
            Add(_psar);
            Add(_st1);
            Add(_st2);
            Add(_st3);
            Add(_adx);
            Add(_kama9);
            Add(_VWAP);
            Add(_kama21);
            Add(_atr);
            Add(_hma);
            Add(SI);
        }

        #endregion

        #region INDICATORS

        private readonly VWAP _VWAP = new VWAP() { VWAPOnly = true, Type = VWAP.VWAPPeriodType.Daily, TWAPMode = VWAP.VWAPMode.VWAP, VolumeMode = VWAP.VolumeType.Total, Period = 300 };
        private readonly EMA Ema200 = new EMA() { Period = 200 };

        private readonly SMA _Sshort = new SMA() { Period = 3 };
        private readonly SMA _Slong = new SMA() { Period = 10 };
        private readonly SMA _Ssignal = new SMA() { Period = 16 };

        private readonly StackedImbalance SI = new StackedImbalance();
        private readonly RSI _rsi = new() { Period = 14 };
        private readonly ATR _atr = new() { Period = 14 };
        private readonly AwesomeOscillator _ao = new AwesomeOscillator();
        private readonly ParabolicSAR _psar = new ParabolicSAR();
        private readonly ADX _adx = new ADX() { Period = 10 };
        private readonly EMA _myEMA = new EMA() { Period = 21 };
        private readonly EMA _9 = new EMA() { Period = 9 };
        private readonly EMA _21 = new EMA() { Period = 21 };
        private readonly HMA _hma = new HMA() { };
        private readonly EMA fastEma = new EMA() { Period = 20 };
        private readonly EMA slowEma = new EMA() { Period = 40 };
        private readonly FisherTransform _ft = new FisherTransform() { Period = 10 };
        private readonly SuperTrend _st1 = new SuperTrend() { Period = 10, Multiplier = 1m };
        private readonly SuperTrend _st2 = new SuperTrend() { Period = 11, Multiplier = 2m };
        private readonly SuperTrend _st3 = new SuperTrend() { Period = 12, Multiplier = 3m };
        private readonly BollingerBands _bb = new BollingerBands() { Period = 20, Shift = 0, Width = 2 };
        private readonly KAMA _kama9 = new KAMA() { ShortPeriod = 2, LongPeriod = 109, EfficiencyRatioPeriod = 9 };
        private readonly KAMA _kama21 = new KAMA() { ShortPeriod = 2, LongPeriod = 109, EfficiencyRatioPeriod = 21 };
        private readonly T3 _t3 = new T3() { Period = 10, Multiplier = 1 };
        private readonly SqueezeMomentum _sq = new SqueezeMomentum() { BBPeriod = 20, BBMultFactor = 2, KCPeriod = 20, KCMultFactor = 1.5m, UseTrueRange = false };

        #endregion

        #region RENDER CONTEXT

        private void DrawString(RenderContext context, string renderText, int yPrice, Color color)
        {
            var textSize = context.MeasureString(renderText, new RenderFont("Arial", 9));
            context.DrawString(renderText, new RenderFont("Arial", 9), color, 
                Container.Region.Right - textSize.Width - 5, yPrice - textSize.Height);
        }

        protected override void OnRender(RenderContext context, DrawingLayouts layout)
        {
            if (ChartInfo is null || InstrumentInfo is null)
                return;

            if (bShowFirstHour)
            {
                var xH = ChartInfo.PriceChartContainer.GetXByBar(_highBar, false);
                var yH = ChartInfo.PriceChartContainer.GetYByPrice(_highest, false);
                context.DrawLine(defibPen.RenderObject, xH, yH, Container.Region.Right, yH);
                DrawString(context, _highS, yH, defibPen.RenderObject.Color);

                var xL = ChartInfo.PriceChartContainer.GetXByBar(_lowBar, false);
                var yL = ChartInfo.PriceChartContainer.GetYByPrice(_lowest, false);
                context.DrawLine(defibPen.RenderObject, xL, yL, Container.Region.Right, yL);
                DrawString(context, _lowS, yL, defibPen.RenderObject.Color);
            }

            if (bShowLondon)
            {
                defibPen.Color = DefaultColors.Lime.Convert();
                var xH = ChartInfo.PriceChartContainer.GetXByBar(_highBarL, false);
                var yH = ChartInfo.PriceChartContainer.GetYByPrice(_highestL, false);
                context.DrawLine(defibPen.RenderObject, xH, yH, Container.Region.Right, yH);
                DrawString(context, _highL, yH, defibPen.RenderObject.Color);

                var xL = ChartInfo.PriceChartContainer.GetXByBar(_lowBarL, false);
                var yL = ChartInfo.PriceChartContainer.GetYByPrice(_lowestL, false);
                context.DrawLine(defibPen.RenderObject, xL, yL, Container.Region.Right, yL);
                DrawString(context, _lowL, yL, defibPen.RenderObject.Color);
            }

            if (!bShowEvil && !bShowNews && !bShowStar && !bAdvanced && !bShowRevPattern)
                return;

            FontSetting Font = new("Arial", iFontSize);
            var renderString = "Howdy";
            var stringSize = context.MeasureString(renderString, Font.RenderObject);
            int x4 = 0;
            int y4 = 0;

            for (var bar = FirstVisibleBarNumber; bar <= LastVisibleBarNumber; bar++)
            {
                renderString = bar.ToString(CultureInfo.InvariantCulture);
                stringSize = context.MeasureString(renderString, Font.RenderObject);

                //                foreach (bars ix in lsBar)
                {
                    if (bShowEvil)
                    {
                        String Evil = EvilTimes(bar);
                        if (Evil != "" && lastEvil != Evil && bShowEvil)
                        {
                            Font.Bold = false;
                            stringSize = context.MeasureString(Evil, Font.RenderObject);
                            x4 = ChartInfo.GetXByBar(bar, false);
                            y4 = Container.Region.Height - stringSize.Height - 40;
                            context.DrawString(Evil, Font.RenderObject, Color.AliceBlue, x4, y4, _format);
                            lastEvil = Evil;
                            Font.Bold = false;
                        }
                    }

                    if (bShowStar)
                    {
                        Color bitches = StarTimes(bar);
                        if (bitches != Color.White && lastColor != bitches && bShowStar)
                        {
                            Font.Bold = true;
                            if (bitches == Color.FromArgb(252, 58, 58))
                                renderString = "MANIPULATION";
                            else
                                renderString = "DISTRIBUTION";
                            stringSize = context.MeasureString(renderString, Font.RenderObject);
                            x4 = ChartInfo.GetXByBar(bar, false);
                            y4 = Container.Region.Height - stringSize.Height - 10;
                            context.DrawString(renderString, Font.RenderObject, bitches, x4, y4, _format);
                            lastColor = bitches;
                            Font.Bold = false;
                        }
                    }
                }

                var font2 = new RenderFont("Arial", iNewsFont);
                var fontB = new RenderFont("Arial", iNewsFont, FontStyle.Bold);
                int upY = 50;
                int upX = ChartArea.Width - 250;
                int iTrades = 0;

                if (bShowNews)
                {
                    RenderFont font;
                    Size textSize;
                    int currY = 40;

                    font = new RenderFont("Arial", iNewsFont + 2);
                    textSize = context.MeasureString("Today's News:", font);
                    context.DrawString("Today's News:", font, Color.YellowGreen, 50, currY);
                    currY += textSize.Height + 10;
                    font = new RenderFont("Arial", iNewsFont);

                    foreach (string s in lsH)
                    {
                        textSize = context.MeasureString(s, font);
                        context.DrawString("High - " + s, font, Color.DarkOrange, 50, currY);
                        currY += textSize.Height;
                    }
                    currY += 9;
                    foreach (string s in lsM)
                    {
                        textSize = context.MeasureString(s, font);
                        context.DrawString("Med  - " + s, font, Color.Gray, 50, currY);
                        currY += textSize.Height;
                    }
                }

            }
        }

        protected void DrawText(int bBar, String strX, Color cI, Color cB, bool bOverride = false, bool bSwap = false)
        {
            var candle = GetCandle(bBar);
            bars ty;

            decimal _tick = ChartInfo.PriceChartContainer.Step;
            decimal loc = 0;

            if (candle.Close > candle.Open || bOverride)
                loc = candle.High + (_tick * iOffset);
            else
                loc = candle.Low - (_tick * iOffset);

            if (candle.Close > candle.Open && bSwap)
                loc = candle.Low - (_tick * (iOffset * 2));
            else if (candle.Close < candle.Open && bSwap)
                loc = candle.High + (_tick * iOffset);

            if (strX == "▼")
                loc = candle.High + (_tick * iOffset);
            if (strX == "▲")
                loc = candle.Low - (_tick * (iOffset * 2));

            AddText("Aver" + bBar, strX, true, bBar, loc, cI, cB, iFontSize, DrawingText.TextAlign.Center);
        }

        #endregion

        #region DATA SERIES

        [Display(Name = "Font Size", GroupName = "Drawing", Order = int.MaxValue)]
        [Range(1, 90)]
        public int TextFont { get => iFontSize; set { iFontSize = value; RecalculateValues(); } }

        [Display(Name = "Text Offset", GroupName = "Drawing", Order = int.MaxValue)]
        [Range(0, 900)]
        public int Offset { get => iOffset; set { iOffset = value; RecalculateValues(); } }

        private RangeDataSeries _upCloud = new("Up Cloud") { RangeColor = MColor.FromArgb(73, 0, 255, 0), DrawAbovePrice = false };
        private RangeDataSeries _dnCloud = new("Down Cloud") { RangeColor = MColor.FromArgb(73, 255, 0, 0), DrawAbovePrice = false };
        private ValueDataSeries _dnTrend = new("Down SuperTrend") { VisualType = VisualMode.Square, Color = DefaultColors.Red.Convert(), Width = 2 };
        private ValueDataSeries _upTrend = new("Up SuperTrend") { Color = DefaultColors.Blue.Convert(), Width = 2, VisualType = VisualMode.Square, ShowZeroValue = false };
        private readonly ValueDataSeries _squeezie = new("Squeeze Relaxer") { Color = MColors.Yellow, VisualType = VisualMode.Dots, Width = 3 };
        private readonly ValueDataSeries _nine21 = new("9 21 cross") { Color = MColor.FromArgb(255, 0, 255, 0), VisualType = VisualMode.Block, Width = 4 };
        private readonly ValueDataSeries _posWhite = new("Vol Imbalance Sell") { Color = MColors.White, VisualType = VisualMode.DownArrow, Width = 1 };
        private readonly ValueDataSeries _negWhite = new("Vol Imbalance Buy") { Color = MColors.White, VisualType = VisualMode.UpArrow, Width = 1 };

        private readonly ValueDataSeries _posRev = new("MACD/PSAR Buy Arrow") { Color = MColors.LightGreen, VisualType = VisualMode.UpArrow, Width = 2 };
        private readonly ValueDataSeries _negRev = new("MACD/PSAR Sell Arrow") { Color = MColors.LightPink, VisualType = VisualMode.DownArrow, Width = 2 };

        private readonly ValueDataSeries _posBBounce = new("Bollinger Bounce Up") { Color = MColors.LightGreen, VisualType = VisualMode.Block, Width = 9 };
        private readonly ValueDataSeries _negBBounce = new("Bollinger Bounce Down") { Color = MColors.LightPink, VisualType = VisualMode.Block, Width = 9 };

        private readonly ValueDataSeries _posSeries = new("Regular Buy Signal") { Color = MColor.FromArgb(255, 0, 255, 0), VisualType = VisualMode.Dots, Width = 2 };
        private readonly ValueDataSeries _negSeries = new("Regular Sell Signal") { Color = MColor.FromArgb(255, 255, 104, 48), VisualType = VisualMode.Dots, Width = 2 };

        private readonly ValueDataSeries _lineVWAP = new("VWAP") { Color = MColor.FromArgb(180, 30, 114, 250), VisualType = VisualMode.Line, Width = 4 };
        private readonly ValueDataSeries _lineEMA200 = new("EMA 200") { Color = MColor.FromArgb(255, 165, 166, 164), VisualType = VisualMode.Line, Width = 4 };
        private readonly ValueDataSeries _lineKAMA = new("KAMA 9") { Color = MColor.FromArgb(180, 252, 186, 3), VisualType = VisualMode.Line, Width = 3 };

        #endregion

        #region Stock HTTP Fetch

        private void ParseStockEvents(String result, int bar)
        {
            int iJSONStart = 0;
            int iJSONEnd = -1;
            String sFinalText = String.Empty; String sNews = String.Empty; String name = String.Empty; String impact = String.Empty; String time = String.Empty; String actual = String.Empty; String previous = String.Empty; String forecast = String.Empty;

            try
            {
                iJSONStart = result.IndexOf("window.calendarComponentStates[1] = ");
                iJSONEnd = result.IndexOf("\"}]}],", iJSONStart);
                sFinalText = result.Substring(iJSONStart, iJSONEnd - iJSONStart);
                sFinalText = sFinalText.Replace("window.calendarComponentStates[1] = ", "");
                sFinalText += "\"}]}]}";

                var jsFile = JObject.Parse(sFinalText);
                foreach (JToken j3 in (JArray)jsFile["days"])
                {
                    JToken j2 = j3.SelectToken("events");
                    foreach (JToken j in j2)
                    {
                        name = j["name"].ToString();
                        impact = j["impactTitle"].ToString();
                        time = j["timeLabel"].ToString();
                        actual = j["actual"].ToString();
                        previous = j["previous"].ToString();
                        forecast = j["forecast"].ToString();
                        sNews = time + "     " + name;
                        if (previous.ToString().Trim().Length > 0)
                            sNews += " (Prev: " + previous + ", Forecast: " + forecast + ")";
                        if (impact.Contains("High"))
                            lsH.Add(sNews);
                        if (impact.Contains("Medium"))
                            lsM.Add(sNews);
                    }
                }
            }
            catch { }
        }

        private void LoadStock(int bar)
        {
            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create("https://www.forexfactory.com/calendar?day=today");
                myRequest.Method = "GET";
                myRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.150 Safari/537.36";
                WebResponse myResponse = myRequest.GetResponse();
                StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
                string result = sr.ReadToEnd();
                sr.Close();
                myResponse.Close();
                ParseStockEvents(result, bar);
                bNewsProcessed = true;
            }
            catch { }
        }

        private void play(String s)
        {
            try
            {
                SoundPlayer my_wave_file = new SoundPlayer(sWavDir + @"\" + s + ".wav");
                my_wave_file.PlaySync();
            }
            catch (Exception)            {            }
        }

        #endregion

        protected override void OnCalculate(int bar, decimal value)
        {
            if (bar == 0)
            {
                DataSeries.ForEach(x => x.Clear());
                HorizontalLinesTillTouch.Clear();
                Rectangles.Clear();
                _lastBarCounted = false;
                return;
            }
            if (bar < 6)
                return;

            MarkOpenSession(bar);

            #region CANDLE CALCULATIONS

            iFutureSound = 0;
            var pcandle = GetCandle(bar);
            var candle = GetCandle(bar - 1);
            var pbar = bar - 1;
            var ppbar = bar - 2;
            value = candle.Close;
            var chT = ChartInfo.ChartType;

            if (IsNewSession(bar))
            {
                _highest = candle.High;
                _lowest = candle.Low;
                _highestL = candle.High;
                _lowestL = candle.Low;
            }

            bShowDown = true;
            bShowUp = true;

            decimal _tick = ChartInfo.PriceChartContainer.Step;
            var p1C = GetCandle(pbar - 1);
            var p2C = GetCandle(pbar - 2);
            var p3C = GetCandle(pbar - 3);
            var p4C = GetCandle(pbar - 4);

            var red = candle.Close < candle.Open;
            var green = candle.Close > candle.Open;
            var c0G = candle.Open < candle.Close;
            var c0R = candle.Open > candle.Close;
            var c1G = p1C.Open < p1C.Close;
            var c1R = p1C.Open > p1C.Close;
            var c2G = p2C.Open < p2C.Close;
            var c2R = p2C.Open > p2C.Close;
            var c3G = p3C.Open < p3C.Close;
            var c3R = p3C.Open > p3C.Close;
            var c4G = p4C.Open < p4C.Close;
            var c4R = p4C.Open > p4C.Close;

            var c0Body = Math.Abs(candle.Close - candle.Open);
            var c1Body = Math.Abs(p1C.Close - p1C.Open);
            var c2Body = Math.Abs(p2C.Close - p2C.Open);
            var c3Body = Math.Abs(p3C.Close - p3C.Open);
            var c4Body = Math.Abs(p4C.Close - p4C.Open);

            var bShaved = red && candle.Close == candle.Low ? true : green && candle.Close == candle.High ? true : false;

            var upWickLarger = c0R && Math.Abs(candle.High - candle.Open) > Math.Abs(candle.Low - candle.Close);
            var downWickLarger = c0G && Math.Abs(candle.Low - candle.Open) > Math.Abs(candle.Close - candle.High);

            var ThreeOutUp = c2R && c1G && c0G && p1C.Open < p2C.Close && p2C.Open < p1C.Close && Math.Abs(p1C.Open - p1C.Close) > Math.Abs(p2C.Open - p2C.Close) && candle.Close > p1C.Low;

            var ThreeOutDown = c2G && c1R && c0R && p1C.Open > p2C.Close && p2C.Open > p1C.Close && Math.Abs(p1C.Open - p1C.Close) > Math.Abs(p2C.Open - p2C.Close) && candle.Close < p1C.Low;

            decimal deltaIntense = 0;
            if (!candle.MaxDelta.Equals(null) && !candle.MinDelta.Equals(null) && !candle.Delta.Equals(null))
            {
                var candleSeconds = Convert.ToDecimal((candle.LastTime - candle.Time).TotalSeconds);
                if (candleSeconds is 0)
                    candleSeconds = 1;
                var volPerSecond = candle.Volume / candleSeconds;
                var deltaPer = candle.Delta > 0 ? (candle.Delta / candle.MaxDelta) : (candle.Delta / candle.MinDelta);
                deltaIntense = Math.Abs((candle.Delta * deltaPer) * volPerSecond);
                var deltaShaved = candle.Delta * deltaPer;

                // Delta Divergence
                if ((c0G && candle.Delta < 0) || (c0R && candle.Delta > 0))
                    iFutureSound = 22;
            }

            #endregion

            #region INDICATORS CALCULATE

            _myEMA.Calculate(pbar, value);
            _t3.Calculate(pbar, value);
            fastEma.Calculate(pbar, value);
            slowEma.Calculate(pbar, value);
            _9.Calculate(pbar, value);
            _21.Calculate(pbar, value);

            _bb.Calculate(pbar, value);
            _rsi.Calculate(pbar, value);
            Ema200.Calculate(pbar, value);

            var e200 = ((ValueDataSeries)Ema200.DataSeries[0])[pbar];
            var vwap = ((ValueDataSeries)_VWAP.DataSeries[0])[pbar];
            var kama9 = ((ValueDataSeries)_kama9.DataSeries[0])[pbar];

            var ao = ((ValueDataSeries)_ao.DataSeries[0])[pbar];
            var t3 = ((ValueDataSeries)_t3.DataSeries[0])[pbar];
            var fast = ((ValueDataSeries)fastEma.DataSeries[0])[pbar];
            var fastM = ((ValueDataSeries)fastEma.DataSeries[0])[pbar - 1];
            var fastN = ((ValueDataSeries)fastEma.DataSeries[0])[pbar - 2];
            var slow = ((ValueDataSeries)slowEma.DataSeries[0])[pbar];
            var slowM = ((ValueDataSeries)slowEma.DataSeries[0])[pbar - 1];
            var slowN = ((ValueDataSeries)slowEma.DataSeries[0])[pbar - 2];
            var sq1 = ((ValueDataSeries)_sq.DataSeries[0])[pbar];
            var sq2 = ((ValueDataSeries)_sq.DataSeries[1])[pbar];
            var psq1 = ((ValueDataSeries)_sq.DataSeries[0])[pbar - 1];
            var psq2 = ((ValueDataSeries)_sq.DataSeries[1])[pbar - 1];
            var ppsq1 = ((ValueDataSeries)_sq.DataSeries[0])[pbar - 2];
            var ppsq2 = ((ValueDataSeries)_sq.DataSeries[1])[pbar - 2];
            var f1 = ((ValueDataSeries)_ft.DataSeries[0])[pbar];
            var f2 = ((ValueDataSeries)_ft.DataSeries[1])[pbar];
            var stu1 = ((ValueDataSeries)_st1.DataSeries[0])[pbar];
            var stu2 = ((ValueDataSeries)_st2.DataSeries[0])[pbar];
            var stu3 = ((ValueDataSeries)_st3.DataSeries[0])[pbar];
            var std1 = ((ValueDataSeries)_st1.DataSeries[1])[pbar];
            var std2 = ((ValueDataSeries)_st2.DataSeries[1])[pbar];
            var std3 = ((ValueDataSeries)_st3.DataSeries[1])[pbar];
            var x = ((ValueDataSeries)_adx.DataSeries[0])[pbar];
            var nn = ((ValueDataSeries)_9.DataSeries[0])[pbar];
            var prev_nn = ((ValueDataSeries)_9.DataSeries[0])[pbar - 1];
            var twone = ((ValueDataSeries)_21.DataSeries[0])[pbar];
            var prev_twone = ((ValueDataSeries)_21.DataSeries[0])[pbar - 1];
            var myema = ((ValueDataSeries)_myEMA.DataSeries[0])[pbar];
            var psar = ((ValueDataSeries)_psar.DataSeries[0])[pbar];
            var ppsar = ((ValueDataSeries)_psar.DataSeries[0])[bar];
            var bb_mid = ((ValueDataSeries)_bb.DataSeries[0])[pbar]; // mid
            var bb_top = ((ValueDataSeries)_bb.DataSeries[1])[pbar]; // top
            var bb_bottom = ((ValueDataSeries)_bb.DataSeries[2])[pbar]; // bottom
            var rsi = ((ValueDataSeries)_rsi.DataSeries[0])[pbar];
            var rsi1 = ((ValueDataSeries)_rsi.DataSeries[0])[pbar - 1];
            var rsi2 = ((ValueDataSeries)_rsi.DataSeries[0])[pbar - 2];
            var hma = ((ValueDataSeries)_hma.DataSeries[0])[pbar];
            var phma = ((ValueDataSeries)_hma.DataSeries[0])[pbar - 1];
            var stack = ((ValueDataSeries)SI.DataSeries[0])[pbar];

            // Linda MACD
            var macd = _Sshort.Calculate(pbar, value) - _Slong.Calculate(pbar, value);
            var signal = _Ssignal.Calculate(pbar, macd);
            var m3 = macd - signal;

            var hullUp = hma > phma;
            var hullDown = hma < phma;
            var fisherUp = (f1 < f2);
            var fisherDown = (f2 < f1);
            var macdUp = (macd > signal);
            var macdDown = (macd < signal);

            var psarBuy = (psar < candle.Close);
            var ppsarBuy = (ppsar < pcandle.Close);
            var psarSell = (psar > candle.Close);
            var ppsarSell = (ppsar > pcandle.Close);

            var eqHigh = c0R && c1R && c2G && c3G && (p1C.High > bb_top || p2C.High > bb_top) &&
                candle.Close < p1C.Close &&
                (p1C.Open == p2C.Close || p1C.Open == p2C.Close + _tick || p1C.Open + _tick == p2C.Close);

            var eqLow = c0G && c1G && c2R && c3R && (p1C.Low < bb_bottom || p2C.Low < bb_bottom) &&
                candle.Close > p1C.Close &&
                (p1C.Open == p2C.Close || p1C.Open == p2C.Close + _tick || p1C.Open + _tick == p2C.Close);

            var t1 = ((fast - slow) - (fastM - slowM)) * iWaddaSensitivity;
            var prevT1 = ((fastM - slowM) - (fastN - slowN)) * iWaddaSensitivity;
            var s1 = bb_top - bb_bottom;

            #endregion

            var bDoji = false;
            var bpDoji = false;

            if (c0G && Math.Abs(candle.Open - candle.Low) > c0Body && Math.Abs(candle.Close - candle.High) > c0Body)
                bDoji = true;
            if (c1G && Math.Abs(p1C.Open - p1C.Low) > c1Body && Math.Abs(p1C.Close - p1C.High) > c1Body)
                bpDoji = true;

            if (c0R && Math.Abs(candle.Close - candle.Low) > c0Body && Math.Abs(candle.Open - candle.High) > c0Body)
                bDoji = true;
            if (c1R && Math.Abs(p1C.Close - p1C.Low) > c1Body && Math.Abs(p1C.Open - p1C.High) > c1Body)
                bpDoji = true;

            if (rsi > 70 && rsi1 < 70)
                iFutureSound = 20;
            if (rsi < 30 && rsi1 > 30)
                iFutureSound = 21;

            var bEMA200Bounce = false;
            var bVWAPBounce = false;
            var bKAMABounce = false;
            decimal c0UpWick = 0;
            decimal c0DownWick = 0;

            if (c0G)
            {
                bEMA200Bounce = (candle.Low < e200 && candle.Open > e200); // (candle.High > e200 && value < e200) ||
                bVWAPBounce = (candle.Low < vwap && candle.Open > vwap); // (candle.High > vwap && value < vwap) || 
                bKAMABounce = (candle.Low < kama9 && candle.Open > kama9);
                c0UpWick = Math.Abs(candle.High - candle.Close);
                c0DownWick = Math.Abs(candle.Low - candle.Open);
            }
            else if (c0R)
            {
                bEMA200Bounce = (candle.High > e200 && candle.Open < e200); // || (candle.Low < e200 && value > e200);
                bVWAPBounce = (candle.High > vwap && candle.Open < vwap); //  || (candle.Low < vwap && value > vwap);
                bKAMABounce = (candle.High > kama9 && candle.Open < kama9);
                c0UpWick = Math.Abs(candle.High - candle.Open);
                c0DownWick = Math.Abs(candle.Low - candle.Close);
            }

            if (bEMA200Bounce || bVWAPBounce)
            {
                iFutureSound = 1;
                _paintBars[pbar] = MColor.FromRgb(255, 255, 255);
            }

            if (bKAMABounce && bKAMAWick)
            {
                iFutureSound = 9;
                _paintBars[pbar] = MColor.FromRgb(255, 255, 255);
            }

            if (bShowWaddahLine)
            {
                _upCloud[pbar].Upper = 0;
                _upCloud[pbar].Lower = 0;
                _dnCloud[pbar].Upper = 0;
                _dnCloud[pbar].Lower = 0;

                if (t1 > 0 && t1 > prevT1 && t1 > s1) // && !bWadGreen)
                {
                    _upCloud[pbar].Upper = _kama9[pbar] + 500;
                    _upCloud[pbar].Lower = _kama9[pbar] - 500;
                    //bWadGreen = true;
                }
                if (t1 <= 0 && Math.Abs(t1) > s1) // && bWadGreen) // && Math.Abs(t1) > Math.Abs(prevT1)
                {
                    _dnCloud[pbar].Upper = _kama9[pbar] + 500;
                    _dnCloud[pbar].Lower = _kama9[pbar] - 500;
                    //bWadGreen = false;
                }
            }

            #region BUY / SELL

            if (bVolumeImbalances)
            {
                var highPen = new Pen(new SolidBrush(Color.CornflowerBlue)) { Width = 3 };
                if (green && c1G && candle.Open > p1C.Close)
                {
                    HorizontalLinesTillTouch.Add(new LineTillTouch(pbar, candle.Open, highPen));
                    _negWhite[pbar] = candle.Low - (_tick * 2);
                    iFutureSound = 12;
                }
                if (red && c1R && candle.Open < p1C.Close)
                {
                    HorizontalLinesTillTouch.Add(new LineTillTouch(pbar, candle.Open, highPen));
                    _posWhite[pbar] = candle.High + (_tick * 2);
                    iFutureSound = 12;
                }
            }

            int iDoubleDecker = 0;
            if (bShowSquare)
            {
                var upTrades = candle.Volume * (candle.Close - candle.Low) / (candle.High - candle.Low);
                var dnTrades = candle.Volume * (candle.High - candle.Close) / (candle.High - candle.Low);
                var pupTrades = pcandle.Volume * (pcandle.Close - pcandle.Low) / (pcandle.High - pcandle.Low);
                var pdnTrades = pcandle.Volume * (pcandle.High - pcandle.Close) / (pcandle.High - pcandle.Low);

                if (upTrades > pdnTrades && upTrades > pupTrades && upTrades > dnTrades && candle.Low < bb_bottom)
                {
                    _posBBounce[pbar] = candle.Low - (_tick * iOffset * 2);
                    iDoubleDecker = 1;
                }
                if (dnTrades > pupTrades && dnTrades > pdnTrades && dnTrades > upTrades && candle.High > bb_top)
                {
                    _negBBounce[pbar] = candle.High + (_tick * iOffset * 2);
                    iDoubleDecker = -1;
                }
            }

            if ((candle.Delta < iMinDelta) || (!macdUp && bUseMACD) || (psarSell && bUsePSAR) || (!fisherUp && bUseFisher) || (value < t3 && bUseT3) || (value < kama9 && bUseKAMA) || (value < myema && bUseMyEMA) || (t1 < 0 && bUseWaddah) || (ao < 0 && bUseAO) || (stu2 == 0 && bUseSuperTrend) || (sq1 < 0 && bUseSqueeze) || x < iMinADX || (bUseHMA && hullDown))
                bShowUp = false;

            if (bShowUp && bShowRegularBuySell)
            {
                if (bIgnoreBadWicks)
                    if ((c0UpWick > (c0Body * 3) && c0DownWick < c0Body) || bDoji)
                        return;
                _posSeries[pbar] = candle.Low - (_tick * iOffset);
                iFutureSound = 10;
            }

            if ((candle.Delta > (iMinDelta * -1)) || (psarBuy && bUsePSAR) || (!macdDown && bUseMACD) || (!fisherDown && bUseFisher) || (value > kama9 && bUseKAMA) || (value > t3 && bUseT3) || (value > myema && bUseMyEMA) || (t1 >= 0 && bUseWaddah) || (ao > 0 && bUseAO) || (std2 == 0 && bUseSuperTrend) || (sq1 > 0 && bUseSqueeze) || x < iMinADX || (bUseHMA && hullUp))
                bShowDown = false;

            if (bShowDown && bShowRegularBuySell)
            {
                if (bIgnoreBadWicks)
                    if ((c0DownWick > (c0Body * 3) && c0UpWick < c0Body) || bDoji)
                        return;
                _negSeries[pbar] = candle.High + _tick * iOffset;
                iFutureSound = 11;
            }

            if (canColor > 1)
            {
                var waddah = Math.Min(Math.Abs(t1) + 70, 255);
                if (canColor == 2)
                    _paintBars[pbar] = t1 > 0 ? MColor.FromArgb(255, 0, (byte)waddah, 0) : MColor.FromArgb(255, (byte)waddah, 0, 0);

                var filteredSQ = Math.Min(Math.Abs(sq1 * 25), 255);
                if (canColor == 3)
                    _paintBars[pbar] = sq1 > 0 ? MColor.FromArgb(255, 0, (byte)filteredSQ, 0) : MColor.FromArgb(255, (byte)filteredSQ, 0, 0);

                var filteredDelta = Math.Min(Math.Abs(candle.Delta), 255);
                if (canColor == 4)
                    _paintBars[pbar] = candle.Delta > 0 ? MColor.FromArgb(255, 0, (byte)filteredDelta, 0) : MColor.FromArgb(255, (byte)filteredDelta, 0, 0);

                var filteredLinda = Math.Min(Math.Abs(m3 * iMACDSensitivity), 255);
                if (canColor == 5)
                    _paintBars[pbar] = m3 > 0 ? MColor.FromArgb(255, 0, (byte)filteredLinda, 0) : MColor.FromArgb(255, (byte)filteredLinda, 0, 0);
            }

            #endregion

            #region ADVANCED LOGIC

            if (deltaIntense > iBigTrades)
            {
                iFutureSound = 4;
                //_paintBars[pbar] = MColor.FromRgb(255, 255, 255);
            }

            int iLocalTouch = 0;
            foreach(LineTillTouch ltt in HorizontalLinesTillTouch)
                if (ltt.Finished)
                    iLocalTouch++;

            if (iLocalTouch > iTouched)
            {
                iTouched = iLocalTouch;
                // _paintBars[bar] = MColor.FromRgb(255, 255, 255);
                bVolImbFinished = true;
            }
            
            if (bShowDojiCity && bDoji && bpDoji)
            {
                iFutureSound = 13;
                var highPen = new Pen(new SolidBrush(Color.Transparent)) { Width = 2 };
                //_paintBars[pbar] = MColor.FromRgb(255, 255, 255);
                Rectangles.Add(new DrawingRectangle(ppbar, p1C.Low - 499, pbar, p1C.High + 499, highPen, 
                    new SolidBrush(Color.FromArgb(255, 47, 47, 47)))); 
            }
            
            if (bShowShaved)
            {
                if (Math.Abs(candle.High - candle.Close) < iShavedRatio && c0G && c0Body > Math.Abs(candle.Low - candle.Open))
                    _paintBars[pbar] = colorShavedg;
                else if (Math.Abs(candle.Low - candle.Close) < iShavedRatio && c0R && c0Body > Math.Abs(candle.High - candle.Open))
                    _paintBars[pbar] = colorShavedr;
            }

            if (bShowEngBB)
            {
                var gPen = new Pen(new SolidBrush(Color.Transparent)) { Width = 3 };
                var rPen = new Pen(new SolidBrush(Color.Transparent)) { Width = 3 };

                if ((candle.Low < bb_bottom || p1C.Low < bb_bottom || p2C.Low < bb_bottom) && c0Body > c1Body && c0G && c1R && candle.Close > p1C.Open)
                {
                    Rectangles.Add(new DrawingRectangle(pbar, p1C.Low - 499, pbar, p1C.High + 499, gPen, new SolidBrush(colorEngulfg)));
                    iFutureSound = 17;
                }
                else if ((candle.High > bb_top || p1C.High > bb_top || p2C.High > bb_top) && c0Body > c1Body && c0R && c1G && candle.Open < p1C.Close)
                {
                    Rectangles.Add(new DrawingRectangle(pbar, p1C.Low - 499, pbar, p1C.High + 499, rPen, new SolidBrush(colorEngulfr)));
                    iFutureSound = 17;
                }
            }

            if (bShowLines)
            {
                _lineEMA200[pbar] = e200;
                _lineKAMA[pbar] = kama9;
                _lineVWAP[pbar] = vwap;
            }

            if (bShowTripleSupertrend)
            {
                var atr = _atr[pbar];
                var median = (candle.Low + candle.High) / 2;
                var dUpperLevel = median + atr * 1.7m;
                var dLowerLevel = median - atr * 1.7m;

                if ((std1 != 0 && std2 != 0) || (std3 != 0 && std2 != 0) || (std3 != 0 && std1 != 0))
                {
                    _dnTrend[pbar] = dUpperLevel;
                    if (_dnTrend[pbar-1] == dLowerLevel)
                        iFutureSound = 15;
                }
                else if ((stu1 != 0 && stu2 != 0) || (stu3 != 0 && stu2 != 0) || (stu1 != 0 && stu3 != 0))
                {
                    _upTrend[pbar] = dLowerLevel;
                    if (_upTrend[pbar - 1] == dUpperLevel)
                        iFutureSound = 14;
                }
            }

            // Squeeze momentum relaxer show
            if (sq1 > 0 && sq1 < psq1 && psq1 > ppsq1 && bShowSqueeze)
            {
                DrawText(pbar, "▼", Color.Yellow, Color.Transparent, false, true); // "▲" "▼"
                iFutureSound = 5;
            }
                
            if (sq1 < 0 && sq1 > psq1 && psq1 < ppsq1 && bShowSqueeze)
            {
                DrawText(pbar, "▲", Color.Yellow, Color.Transparent, false, true);
                iFutureSound = 5;
            }

            // 9/21 cross show
            if (nn > twone && prev_nn <= prev_twone && bShow921)
                DrawText(pbar, "X", Color.Yellow, Color.Transparent, false, true);
            if (nn < twone && prev_nn >= prev_twone && bShow921)
                DrawText(pbar, "X", Color.Yellow, Color.Transparent, false, true);

            if (bAdvanced)
            {
                if (c4Body > c3Body && c3Body > c2Body && c2Body > c1Body && c1Body > c0Body)
                    if ((candle.Close > p1C.Close && p1C.Close > p2C.Close && p2C.Close > p3C.Close) ||
                    (candle.Close < p1C.Close && p1C.Close < p2C.Close && p2C.Close < p3C.Close))
                {
                        DrawText(pbar, "Stairs", Color.Yellow, Color.Transparent);
                        iFutureSound = 4;
                }

                if (eqHigh)
                {
                    DrawText(pbar - 1, "Eq Hi", Color.Lime, Color.Transparent, false, true);
                    iFutureSound = 6;
                }

                if (eqLow)
                {
                    DrawText(pbar - 1, "Eq Low", Color.Yellow, Color.Transparent, false, true);
                    iFutureSound = 7;
                }
            }

            if (bShowRevPattern)
            {
                //if (c0R && candle.High > bb_top && candle.Open < bb_top && candle.Open > p1C.Close && upWickLarger)
                //    DrawText(pbar, "Wick", Color.Yellow, Color.Transparent, false, true);
                //if (c0G && candle.Low < bb_bottom && candle.Open > bb_bottom && candle.Open > p1C.Close && downWickLarger)
                //    DrawText(pbar, "Wick", Color.Yellow, Color.Transparent, false, true);

                if (c0G && c1R && c2R && VolSec(p1C) > VolSec(p2C) && VolSec(p2C) > VolSec(p3C) && candle.Delta < 0)
                {
                    DrawText(pbar, "Vol\nRev", Color.Yellow, Color.Transparent, false, true);
                    iFutureSound = 2;
                }

                if (c0R && c1G && c2G && VolSec(p1C) > VolSec(p2C) && VolSec(p2C) > VolSec(p3C) && candle.Delta > 0)
                {
                    DrawText(pbar, "Vol\nRev", Color.Lime, Color.Transparent, false, true);
                    iFutureSound = 2;
                }

                if (ThreeOutUp)
                    DrawText(pbar, "3oU", Color.Yellow, Color.Transparent);
                if (ThreeOutDown && bShowRevPattern)
                    DrawText(pbar, "3oD", Color.Yellow, Color.Transparent);
            }

            // Nebula cloud
            if (bShowCloud)
                if (_kama9[pbar] > _kama21[pbar])
                {
                    _upCloud[pbar].Upper = _kama9[pbar];
                    _upCloud[pbar].Lower = _kama21[pbar];
                }
                else
                {
                    _dnCloud[pbar].Upper = _kama21[pbar];
                    _dnCloud[pbar].Lower = _kama9[pbar];
                }

            // Trampoline
            if (bShowTramp)
            {
                if (c0R && c1R && candle.Close < p1C.Close && (rsi >= 70 || rsi1 >= 70 || rsi2 >= 70) &&
                    c2G && p2C.High >= (bb_top - (_tick * 30)))
                {
                    iFutureSound = 8;
                    DrawText(pbar, "TR", Color.Yellow, Color.BlueViolet, false, true);
                }
                   
                if (c0G && c1G && candle.Close > p1C.Close && (rsi < 25 || rsi1 < 25 || rsi2 < 25) &&
                    c2R && p2C.Low <= (bb_bottom + (_tick * 30)))
                {
                    iFutureSound = 8;
                    DrawText(pbar, "TR", Color.Yellow, Color.BlueViolet, false, true);
                }
            }

            if (ppsarBuy && m3 > 0 && candle.Delta > 50 && !bBigArrowUp && bShowMACDPSARArrow)
            {
                _posRev[bar] = candle.Low - (_tick * 2);
                bBigArrowUp = true;
            }
            if (ppsarSell && m3 < 0 && candle.Delta < 50 && bBigArrowUp && bShowMACDPSARArrow)
            {
                _negRev[bar] = candle.High + (_tick * 2);
                bBigArrowUp = false;
            }

            #endregion

            #region ALERTS LOGIC

            if (_lastBar != bar)
            {
                if (_lastBarCounted && UseAlerts)
                {
                    var priceString = candle.Close.ToString();

                    switch(iFutureSound)
                    {
                        case 1:
                            play("majorline");
                            Task.Run(() => SendWebhookAndWriteToFile("MAJOR LINE WICKED taco ", InstrumentInfo.Instrument, priceString, "majorline"));
                            break;
                        case 2:
                            play("VolRev");
                            Task.Run(() => SendWebhookAndWriteToFile("VOLUME REVERSED taco", InstrumentInfo.Instrument, priceString, "VolRev"));
                            break;
                        case 3:
                            play("intensity");
                            Task.Run(() => SendWebhookAndWriteToFile("INTENSITY taco", InstrumentInfo.Instrument, priceString, "intensity"));
                            break;
                        case 4:
                            play("stairs");
                            Task.Run(() => SendWebhookAndWriteToFile("STAIRS taco", InstrumentInfo.Instrument, priceString, "stairs"));
                            break;
                        case 5:
                            play("squeezie");
                            Task.Run(() => SendWebhookAndWriteToFile("SQUEEZED taco", InstrumentInfo.Instrument, priceString, "squeezie"));
                            break;
                        case 6:
                            play("equal high");
                            Task.Run(() => SendWebhookAndWriteToFile("EQUAL HIGH taco", InstrumentInfo.Instrument, priceString, "equalhigh"));
                            break;
                        case 7:
                            play("equal low");
                            Task.Run(() => SendWebhookAndWriteToFile("EQUAL LOW taco", InstrumentInfo.Instrument, priceString, "equallow"));
                            break;
                        case 8:
                            play("trampoline");
                            Task.Run(() => SendWebhookAndWriteToFile("TRAMPOLINE taco", InstrumentInfo.Instrument, priceString, "trampoline"));
                            break;
                        case 9:
                            play("kama");
                            Task.Run(() => SendWebhookAndWriteToFile("KAMA BOUNCE taco", InstrumentInfo.Instrument, priceString, "kama"));
                            break;
                        case 10:
                            play("buy");
                            Task.Run(() => SendWebhookAndWriteToFile("BOUGHT taco", InstrumentInfo.Instrument, priceString, "buy"));
                            break;
                        case 11:
                            play("sell");
                            Task.Run(() => SendWebhookAndWriteToFile("SOLD taco", InstrumentInfo.Instrument, priceString, "sell"));
                            break;
                        case 12:
                            play("volimb");
                            Task.Run(() => SendWebhookAndWriteToFile("IMBALANCED chalupa", InstrumentInfo.Instrument, priceString, "volimb"));
                            break;
                        case 13:
                            play("dojicity");
                            Task.Run(() => SendWebhookAndWriteToFile("DOJI CITY chalupa", InstrumentInfo.Instrument, priceString, "dojicity"));
                            break;
                        case 14:
                            play("superGREEN");
                            Task.Run(() => SendWebhookAndWriteToFile("SuperTrend GREEN", InstrumentInfo.Instrument, priceString, ""));
                            break;
                        case 15:
                            play("superRED");
                            Task.Run(() => SendWebhookAndWriteToFile("SuperTrend RED", InstrumentInfo.Instrument, priceString, ""));
                            break;
                        case 16:
                            play("vol2x");
                            Task.Run(() => SendWebhookAndWriteToFile("DOUBLE VOLUME", InstrumentInfo.Instrument, priceString, ""));
                            break;
                        case 17:
                            play("engulf");
                            Task.Run(() => SendWebhookAndWriteToFile("ENGULFING CANDLE", InstrumentInfo.Instrument, priceString, ""));
                            break;
                        case 18:
                            play("choppy");
                            Task.Run(() => SendWebhookAndWriteToFile("CHOPPY WATERS", InstrumentInfo.Instrument, priceString, ""));
                            break;
                        case 19:
                            play("stacked");
                            Task.Run(() => SendWebhookAndWriteToFile("STACKED IMBALANCE", InstrumentInfo.Instrument, priceString, ""));
                            break;
                        case 20:
                            play("rsiOB");
                            Task.Run(() => SendWebhookAndWriteToFile("RSI OVERBOUGHT", InstrumentInfo.Instrument, priceString, ""));
                            break;
                        case 21:
                            play("rsiOS");
                            Task.Run(() => SendWebhookAndWriteToFile("RSI OVERSOLD", InstrumentInfo.Instrument, priceString, ""));
                            break;
                        case 22:
                            play("divergence");
                            Task.Run(() => SendWebhookAndWriteToFile("DELTA DIVERGENCE", InstrumentInfo.Instrument, priceString, ""));
                            break;
                        default: break;
                    }

                    //if (bVolImbFinished)
                    //{
                    //    AddAlert(AlertFile, "Vol Imbalance Finish");
                    //    Task.Run(() => SendWebhookAndWriteToFile("NACHO FRIES ALERT ", InstrumentInfo.Instrument, priceString));
                    //}

                    //if (bVolumeImbalances)
                    //    if ((green && c1G && candle.Open > p1C.Close) || (red && c1R && candle.Open < p1C.Close))
                    //    {
                    //        //AddAlert(AlertFile, "Volume Imbalance");
                    //        Task.Run(() => SendWebhookAndWriteToFile("IMBALANCED a chalupa ", InstrumentInfo.Instrument, priceString));
                    //        play("volimb");
                    //    }

                    //if (iDoubleDecker != 0)
                    //{
                    //    //AddAlert(AlertFile, "Bollinger Signal");
                    //    Task.Run(() => SendWebhookAndWriteToFile("BOLLINGER taco ", InstrumentInfo.Instrument, priceString));
                    //}
                    //if (bShowUp && bShowRegularBuySell)
                    //{
                    //    //AddAlert(AlertFile, "BUY Signal");
                    //    Task.Run(() => SendWebhookAndWriteToFile("BOUGHT a tostada ", InstrumentInfo.Instrument, priceString));
                    //    play("buy");
                    //}
                    //else if (bShowDown && bShowRegularBuySell)
                    //{
                    //    //AddAlert(AlertFile, "SELL Signal");
                    //    Task.Run(() => SendWebhookAndWriteToFile("SOLD a tostada ", InstrumentInfo.Instrument, priceString));
                    //    play("sell");
                    //}

                   // if ((ppsarBuy && m3 > 0 && candle.Delta > 50 && !bBigArrowUp) || (ppsarSell && m3 < 0 && candle.Delta < 50 && bBigArrowUp) && bShowMACDPSARArrow)
                        //AddAlert(AlertFile, "Big Arrow");
                }
                _lastBar = bar;
            }
            else
            {
                if (!_lastBarCounted)
                    _lastBarCounted = true;
            }

            #endregion

            #region CLUSTER LOGIC

            if (bShowClusters)
            {
                var cPL = candle.GetPriceVolumeInfo(candle.Low);
                var cPH = candle.GetPriceVolumeInfo(candle.High);

                var gPen = new Pen(new SolidBrush(Color.Lime)) { Width = 4 };
                var rPen = new Pen(new SolidBrush(Color.Red)) { Width = 4 };

                var vH = Math.Abs(candle.High - candle.ValueArea.ValueAreaHigh);
                var vL = Math.Abs(candle.ValueArea.ValueAreaLow - candle.Low);

                if ((vH * iClusterRatio) < vL && m3 > 0 && green && cPL.Bid < iMinBid && cPL.Ask < iMinAsk)
                    HorizontalLinesTillTouch.Add(new LineTillTouch(pbar, candle.Open, gPen, 1));
                else if ((vL * iClusterRatio) < vH && m3 < 0 && red && cPH.Bid < iMinBid && cPH.Ask < iMinAsk)
                    HorizontalLinesTillTouch.Add(new LineTillTouch(pbar, candle.Open, rPen, 1));
            }

            #endregion

            if (!bNewsProcessed && bShowNews)
                LoadStock(pbar);
        }

        #region MISC FUNCTIONS

        private void MarkOpenSession(int bar)
        {
            var candle = GetCandle(bar);
            var diff = InstrumentInfo.TimeZone;
            var time = candle.Time.AddHours(diff);
            var today = DateTime.Today.Year.ToString() + "-" + DateTime.Today.Month.ToString() + "-" + DateTime.Today.Day.ToString();

            if (time > DateTime.Parse(today + " 08:20AM") && time < DateTime.Parse(today + " 08:29AM"))
            {
                _highest = candle.High;
                _highBar = bar;
                _lowest = candle.Low;
                _lowBar = bar;
            }

            if (time > DateTime.Parse(today + " 08:30AM") && time < DateTime.Parse(today + " 09:30AM"))
            {
                if (candle.High > _highest)
                {
                    _highest = candle.High;
                    _highBar = bar;
                }
                if (candle.Low < _lowest)
                {
                    _lowest = candle.Low;
                    _lowBar = bar;
                }
            }

            // LONDON SESSION TIMES
            if (time > DateTime.Parse(today + " 01:50AM") && time < DateTime.Parse(today + " 01:59AM"))
            {
                _highestL = candle.High;
                _highBarL = bar;
                _lowestL = candle.Low;
                _lowBarL = bar;
            }

            if (time > DateTime.Parse(today + " 02:00AM") && time < DateTime.Parse(today + " 03:00AM"))
            {
                if (candle.High > _highestL)
                {
                    _highestL = candle.High;
                    _highBarL = bar;
                }
                if (candle.Low < _lowestL)
                {
                    _lowestL = candle.Low;
                    _lowBarL = bar;
                }
            }
        }

        private async Task SendWebhook(string message, string ticker, string price)
        {
            DateTime bitches = new DateTime();
            bitches = DateTime.Now;
            bitches = bitches.AddHours(1);
            var fullMessage = $"{message} from {ticker} for ${price} at " + bitches.ToString("h:mm:ss tt") + " EST";
            var payload = new { content = fullMessage };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var token = Environment.GetEnvironmentVariable("Webhook");
            await client.PostAsync(token, content);
        }
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private async Task WriteToTextFile(string file)
        {
            await semaphore.WaitAsync();
            try
            {
                System.Diagnostics.Process.Start("cmd.exe", "/c " + sWavDir + @"\copyimage.bat " + file);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task SendWebhookAndWriteToFile(string message, string ticker, string price, string file)
        {
            var token = Environment.GetEnvironmentVariable("Webhook");
            if (token == "")
                return;

            var sendWebhookTask = SendWebhook(message, ticker, price);
            var writeToTextFileTask = WriteToTextFile(file);

            await Task.WhenAll(sendWebhookTask, writeToTextFileTask);
        }

        private String EvilTimes(int bar)
        {
            var candle = GetCandle(bar);
            var diff = InstrumentInfo.TimeZone;
            var time = candle.Time.AddHours(diff);

            if (time.Hour == 9 && time.Minute >= 00 && time.Minute <= 59)
                return "Market Pivot";

            if (time.Hour == 10 && time.Minute >= 00 && time.Minute <= 29)
                return "Euro Move";

            if (time.Hour == 10 && time.Minute >= 30 && time.Minute <= 59)
                return "Inverse";

            if (time.Hour == 11 && time.Minute >= 00 && time.Minute <= 59)
                return "Inverse ";

            if (time.Hour == 12 && time.Minute >= 00 && time.Minute <= 59)
                return "Bond Auctions";

            if (time.Hour == 13 && time.Minute >= 29 && time.Minute <= 59)
                return "Capital Injection";

            if (time.Hour == 14 && time.Minute >= 29 && time.Minute <= 59)
                return "Capital Injection";

            if (time.Hour == 14 && time.Minute >= 49 && time.Minute <= 59)
                return "Rug Pull";

            return "";
        }

        private Color StarTimes(int bar)
        {
            var candle = GetCandle(bar);
            var diff = InstrumentInfo.TimeZone;
            var time = candle.Time.AddHours(diff);

            // Manipulation
            if (
                (time.Hour == 8 && time.Minute >= 47 && time.Minute <= 59) ||
                (time.Hour == 9 && time.Minute >= 00 && time.Minute <= 11) ||
                (time.Hour == 10 && time.Minute >= 10 && time.Minute <= 26) ||
                (time.Hour == 11 && time.Minute >= 07 && time.Minute <= 19) ||
                (time.Hour == 11 && time.Minute >= 55 && time.Minute <= 59) ||
                (time.Hour == 12 && time.Minute >= 00 && time.Minute <= 07)
                )
                return Color.FromArgb(252, 58, 58);

            // Distribution
            if (
                (time.Hour == 9 && time.Minute >= 11 && time.Minute <= 47) ||
                (time.Hour == 10 && time.Minute >= 26 && time.Minute <= 50) ||
                (time.Hour == 11 && time.Minute >= 19 && time.Minute <= 37) ||
                (time.Hour == 12 && time.Minute >= 07 && time.Minute <= 25)
                )
                return Color.FromArgb(78, 152, 242);

            return Color.White;
        }

        #endregion

    }
}