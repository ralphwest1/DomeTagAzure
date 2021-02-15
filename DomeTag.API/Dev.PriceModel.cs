using System;
using System.Collections.Generic;
using System.Linq;


namespace DomeTag.API
{
    public class PriceService
    {
        public PriceConfig config { get; set; }
        public PriceService() => config = new PriceConfig();
        public PriceResponse CalculatePrice(PriceRequest priceRequest)
        {
            var priceResponse = new PriceResponse(priceRequest);
            GetCalculationItems(priceResponse);
            CalculateBase(priceResponse);
            CalculateSubTotal(ePricingOption.OptionsTotal, ePricingCalculationStep.Options, priceResponse);
            CalculateSubTotal(ePricingOption.StandardTotal, ePricingCalculationStep.Totals, priceResponse);
            CalculateOverride(priceResponse);
            CalculateTotal(priceResponse);
            return priceResponse;
        }
        public void GetCalculationItems(PriceResponse priceResponse)
        {
            priceResponse.CalculationItems = config.Options.FindAll(x => x.OptionGroup == ePricingOptionGroup.BasePrice);
            priceResponse.CalculationItems.AddRange(config.Options.FindAll(c => priceResponse.Request.Options.Select(x => x.Option).Contains(c.Option)));
            //priceResponse.CalculationItems.AddRange(config.Options.FindAll(x => priceResponse.Request.Options.Keys.Contains(x.Option)));
        }
        public void CalculateBase(PriceResponse priceResponse)
        {
            var basePrice = priceResponse.CalculationItems.Find(x => x.OptionGroup == ePricingOptionGroup.BasePrice);
            basePrice.Calculate(priceResponse.Request.Quantity, priceResponse.Request.SquareInchesPerLabel, 0);
            basePrice.PercentAdjustment = CalculateTSIPercent(priceResponse.Request.TotalSquareInches);
            basePrice.Calculate(priceResponse.Request.Quantity, priceResponse.Request.SquareInchesPerLabel, basePrice.PricePerUnit);
            var subTotal = new PriceItem(ePricingOption.BaseTotal, ePricingCalculationStep.Totals);
            AddItemToSubTotal(subTotal, basePrice);
            priceResponse.CalculationItems.Add(subTotal);
            priceResponse.RunningPricePerUnit = basePrice.PricePerUnit;

            //basePrice.Calculate(priceResponse.Request.Quantity, priceResponse.Request.SquareInchesPerLabel, basePrice.PricePerUnit);
            //priceResponse.RunningPricePerUnit = basePrice.PricePerUnit;
        }
        public static PriceItem CalculateSubTotal(ePricingOption option, ePricingCalculationStep step, PriceResponse priceResponse)
        {
            var subTotal = new PriceItem(option, ePricingCalculationStep.Totals);
            var items = priceResponse.CalculationItems.FindAll(x => x.Step == step);
            foreach (PriceItem item in items)
            {
                item.Calculate(priceResponse.Request.Quantity, priceResponse.Request.SquareInchesPerLabel, priceResponse.RunningPricePerUnit);
                AddItemToSubTotal(subTotal, item);
            }
            subTotal.TotalPrice = Math.Round(subTotal.PricePerUnit * priceResponse.Request.Quantity, 2);
            priceResponse.RunningPricePerUnit += subTotal.PricePerUnit;
            priceResponse.CalculationItems.Add(subTotal);
            return subTotal;
        }
        public static void CalculateOverride(PriceResponse priceResponse)
        {

            var overrideOption = priceResponse.Request.Options.Find(x => x.OptionGroup == ePricingOptionGroup.PriceOverride);
            if (overrideOption == null) return;
            PriceItem item = new PriceItem(overrideOption.Option, ePricingCalculationStep.Overrides);
            PriceItem overrideTotal = new PriceItem(ePricingOption.OverrideTotal, ePricingCalculationStep.Totals);
            var StandardPrice = priceResponse.RunningPricePerUnit;
            switch (item.Option)
            {
                case ePricingOption.NoOverrides: return;
                case ePricingOption.AdjustmentOverride:
                    var value = (PriceItem)overrideOption.Value;
                    item.LabelAdjustment = value.LabelAdjustment;
                    item.SquareInchAdjustment = value.SquareInchAdjustment;
                    item.TotalAdjustment = value.TotalAdjustment;
                    item.PercentAdjustment = value.PercentAdjustment;
                    item.Calculate(priceResponse.Request.Quantity, priceResponse.Request.SquareInchesPerLabel, StandardPrice);
                    overrideTotal.PricePerUnit = item.PricePerUnit;
                    break;
                case ePricingOption.NoCharge: break;
                case ePricingOption.PricePerUnitOverride:
                    item.PricePerUnit = (decimal)overrideOption.Value;
                    item.TotalPrice = Math.Round(item.PricePerUnit * priceResponse.Request.Quantity, 2);
                    overrideTotal.PricePerUnit = item.PricePerUnit;
                    overrideTotal.TotalPrice = item.TotalPrice;
                    break;
                case ePricingOption.SamplesOverride:
                    item.TotalPrice = (decimal?)overrideOption.Value ?? 85;
                    item.PricePerUnit = Math.Round(item.TotalPrice / priceResponse.Request.Quantity, 3);
                    overrideTotal.PricePerUnit = item.PricePerUnit;
                    overrideTotal.TotalPrice = item.TotalPrice;
                    break;
                case ePricingOption.TotalPriceOverride:
                    item.TotalPrice = (decimal)overrideOption.Value;
                    item.PricePerUnit = Math.Round(item.TotalPrice / priceResponse.Request.Quantity, 3);
                    overrideTotal.PricePerUnit = item.PricePerUnit;
                    overrideTotal.TotalPrice = item.TotalPrice;
                    break;
                default:
                    //response.PricePerUnit_Adjustments = 0;
                    break;
            }
            priceResponse.CalculationItems.Add(item);
            priceResponse.CalculationItems.Add(overrideTotal);
            priceResponse.OverridePercent = (overrideTotal.PricePerUnit - StandardPrice) / StandardPrice;


        }
        public void CalculateTotal(PriceResponse priceResponse)
        {
            var overrideTotal = priceResponse.CalculationItems.FirstOrDefault(x => x.Option == ePricingOption.OverrideTotal);
            if (overrideTotal != null)
            {
                priceResponse.PricePerUnit = overrideTotal.PricePerUnit;
                priceResponse.TotalPrice = overrideTotal.TotalPrice;
            }
            else
            {
                priceResponse.PricePerUnit = priceResponse.CalculationItems.FirstOrDefault(x => x.Option == ePricingOption.StandardTotal).PricePerUnit;
                priceResponse.TotalPrice = Math.Round(priceResponse.PricePerUnit * priceResponse.Request.Quantity, 2);
            }
            priceResponse.PricePerSquareInch = Math.Round(priceResponse.PricePerUnit / priceResponse.Request.SquareInchesPerLabel, 3);
        }
        public static void AddItemToSubTotal(PriceItem subTotal, PriceItem item)
        {
            subTotal.LabelAdjustment += item.LabelAdjustment;
            subTotal.SquareInchAdjustment += item.SquareInchAdjustment;
            subTotal.TotalAdjustment += item.TotalAdjustment;
            subTotal.PercentAdjustment += item.PercentAdjustment;
            subTotal.LabelAmount += item.LabelAmount;
            subTotal.SquareInchAmount += item.SquareInchAmount;
            subTotal.TotalAmount += item.TotalAmount;
            subTotal.PercentAmount += item.PercentAmount;
            subTotal.PricePerUnit += item.PricePerUnit;
        }
        public decimal CalculateTSIPercent(decimal tsi)
        {
            var tsiTier = config.TSITiers.First(x => x.ValueMax >= tsi);
            var tierPercent = (tsiTier.ValueMax - tsi) / tsiTier.ValueRange;
            var tsiPercent = NumericHelpers.RoundUp3((tierPercent * tsiTier.PerecentRange) + tsiTier.PercentMin);
            return tsiPercent;
        }
        public static decimal CalculateSizeRatioPercent(decimal labelWidth, decimal LabelHeight)
        {
            var min = Math.Min(labelWidth, LabelHeight);
            var max = Math.Max(labelWidth, LabelHeight);
            var ratio = 1 - (min / max);
            return ratio;
        }


        public class PriceRequest
        {
            public decimal Quantity { get; set; }
            public decimal LabelWidth { get; set; }
            public decimal LabelHeight { get; set; }
            //public decimal SquareInchesPerLabel { get; private set; }
            //public decimal TotalSquareInches { get; private set; }
            public decimal SquareInchesPerLabel => LabelWidth * LabelHeight;
            public decimal TotalSquareInches => SquareInchesPerLabel * Quantity;
            public List<PriceOption> Options { get; set; } = new List<PriceOption>();
            public PriceRequest()
            {
                //SquareInchesPerLabel = LabelWidth * LabelHeight;
                //TotalSquareInches = SquareInchesPerLabel * Quantity;
            }
            public PriceRequest(decimal labelWidth, decimal labelHeight, decimal quantity, params Enum[] enumOptions) : this(labelWidth, labelHeight, quantity, enumOptions.ToList()) { }
            public PriceRequest(decimal labelWidth, decimal labelHeight, decimal quantity, List<Enum> enumOptions)
            {
                Quantity = quantity;
                LabelWidth = labelWidth;
                LabelHeight = labelHeight;
                //SquareInchesPerLabel = LabelWidth * LabelHeight;
                //TotalSquareInches = SquareInchesPerLabel * Quantity;
                AddOptions(enumOptions);
            }

            public void AddOption(Enum option, Object obj = null) => Options.Add(new PriceOption(option, obj));
            public void AddOption(string option, Object obj = null) => Options.Add(new PriceOption(option, obj));
            public void AddOptions(List<Enum> options) => options.ForEach(x => AddOption(x));
        }
        public class PriceOption
        {
            public string OptionGroupName => OptionGroup.ToString();
            public string OptionName => Option.ToString();
            public ePricingOptionGroup OptionGroup { get; private set; }
            public ePricingOption Option { get; set; }
            public Object Value { get; set; }
            public PriceOption(ePricingOption item, dynamic value = null)
            {
                Option = item;
                OptionGroup = (ePricingOptionGroup)((int)Option / 100);
                Value = value;
            }
            public PriceOption(Enum item, Object value = null) : this((ePricingOption)Convert.ToInt32(item), value) { }
            public PriceOption(int item, Object value = null) : this((ePricingOption)item, value) { }
            public PriceOption(string item, Object value = null) : this((ePricingOption)Enum.Parse(typeof(ePricingOption), item), value) { }
        }
        public class PriceResponse
        {
            public decimal PricePerUnit { get; set; }
            public decimal TotalPrice { get; set; }
            public decimal PricePerSquareInch { get; set; }
            public decimal OverridePercent { get; set; } = 0;
            internal decimal RunningPricePerUnit { get; set; } = 0;
            public List<PriceSubTotal> SubTotals { get; set; } = new List<PriceSubTotal>();
            public List<PriceItem> CalculationItems { get; set; }
            public PriceRequest Request { get; set; }
            public PriceResponse(PriceRequest request) => Request = request;
        }
        public class PriceSubTotal
        {
            public string SubTotalName => Step.ToString();
            public ePricingCalculationStep Step { get; set; }
            public decimal PricePerUnit { get; set; } = 0;
            public decimal TotalPrice { get; set; } = 0;
            public decimal LabelAdjustment { get; set; } = 0;
            public decimal SquareInchAdjustment { get; set; } = 0;
            public decimal TotalAdjustment { get; set; } = 0;
            public decimal PercentAdjustment { get; set; } = 0;
            public decimal OtherAdjustment { get; set; }
            public decimal LabelAmount { get; set; } = 0;
            public decimal SquareInchAmount { get; set; } = 0;
            public decimal TotalAmount { get; set; } = 0;
            public decimal PercentAmount { get; set; } = 0;
            public PriceSubTotal(ePricingCalculationStep step)
            {
                Step = step;
            }
        }
        public class PriceItem
        {
            public string OptionGroupName => OptionGroup.ToString();
            public string OptionName => Option.ToString();
            public decimal PricePerUnit { get; set; } = 0;
            public decimal TotalPrice { get; set; } = 0;
            public decimal LabelAdjustment { get; set; } = 0;
            public decimal SquareInchAdjustment { get; set; } = 0;
            public decimal TotalAdjustment { get; set; } = 0;
            public decimal PercentAdjustment { get; set; } = 0;
            public decimal LabelAmount { get; set; } = 0;
            public decimal SquareInchAmount { get; set; } = 0;
            public decimal TotalAmount { get; set; } = 0;
            public decimal PercentAmount { get; set; } = 0;
            public ePricingOptionGroup OptionGroup { get; private set; }
            public ePricingOption Option { get; set; }
            public ePricingCalculationStep Step { get; set; }

            public PriceItem(decimal labelAdjustment = 0, decimal squareInchAdjustment = 0, decimal totalAdjustment = 0, decimal percentAdjustment = 0, decimal pricePerUnit = 0, decimal totalPrice = 0)
            {
                LabelAdjustment = labelAdjustment;
                SquareInchAdjustment = squareInchAdjustment;
                TotalAdjustment = totalAdjustment;
                PercentAdjustment = percentAdjustment;
                PricePerUnit = pricePerUnit;
                TotalPrice = totalPrice;
            }

            public PriceItem(int option, int step, decimal labelAdjustment = 0, decimal squareInchAdjustment = 0, decimal totalAdjustment = 0, decimal percentAdjustment = 0)
            {
                Option = (ePricingOption)option;
                OptionGroup = (ePricingOptionGroup)(option / 100);
                Step = (ePricingCalculationStep)step;
                LabelAdjustment = labelAdjustment;
                SquareInchAdjustment = squareInchAdjustment;
                TotalAdjustment = totalAdjustment;
                PercentAdjustment = percentAdjustment;
            }
            public PriceItem(ePricingOption option, ePricingCalculationStep step, decimal label = 0, decimal squareInch = 0, decimal total = 0, decimal percent = 0)
            {
                Option = option;
                OptionGroup = (ePricingOptionGroup)((int)option / 100);
                Step = step;
                LabelAdjustment = label;
                SquareInchAdjustment = squareInch;
                TotalAdjustment = total;
                PercentAdjustment = percent;
            }
            public void Calculate(decimal Labels, decimal squareInchesPerLabel, decimal baseAmount)
            {
                LabelAmount = LabelAdjustment;
                SquareInchAmount = NumericHelpers.RoundUp3(SquareInchAdjustment * squareInchesPerLabel);
                TotalAmount = NumericHelpers.RoundUp3(TotalAdjustment / Labels);
                PercentAmount = NumericHelpers.RoundUp3(PercentAdjustment * baseAmount);
                PricePerUnit = LabelAmount + SquareInchAmount + TotalAmount + PercentAmount;
                TotalPrice = Math.Round(PricePerUnit * Labels, 2);
            }

        }
        public class PriceConfig
        {
            public List<PriceItem> Options { get; set; }
            public List<VariablePercentTier> TSITiers { get; set; }
            public PriceConfig()
            {
                Options = GetCalculationItems();
                TSITiers = GetTSITiers();
            }
            public List<VariablePercentTier> GetTSITiers()
            {
                List<VariablePercentTier> tsiTiers = new List<VariablePercentTier>()
                    {
                    new VariablePercentTier(0, 500, 6.0m, 8.0m),
                    new VariablePercentTier(500, 1000, 5m, 6m),
                    new VariablePercentTier(1000, 5000, 3m, 5m),
                    new VariablePercentTier(5000, 10000, 2.2m, 3m),
                    new VariablePercentTier(10000, 50000, 1, 2m),
                    new VariablePercentTier(50000, 100000, 0.6m, 1m),
                    new VariablePercentTier(100000, 500000, 0.5m, 0.6m),
                    new VariablePercentTier(500000, 1000000, 0.4m, 0.5m),
                    new VariablePercentTier(1000000, 5000000, 0.3m, 0.4m),
                    new VariablePercentTier(5000000, 10000000, 0.25m, 0.3m),
                    new VariablePercentTier(10000000, 100000000, 0.1m, 0.25m),
                    new VariablePercentTier(100000000, 999999999, 0.1m, 0.1m),
                    };
                return tsiTiers;
            }
            public List<PriceItem> GetCalculationItems()
            {
                List<PriceItem> list = new List<PriceItem>()
                {
                    new PriceItem(1000, 10, 0.008m, 0.036m, 0m, 0m),
                    new PriceItem(1010, 20, 0m, 0m, 130m, 0m),
                    new PriceItem(1100, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(1111, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(1121, 20, 0m, 0m, 0m, 0.02m),
                    new PriceItem(1131, 20, 0m, 0m, 0m, 0.02m),
                    new PriceItem(1141, 20, 0m, 0m, 0m, 0.03m),
                    new PriceItem(1151, 20, 0.02m, 0m, 20m, 0.1m),
                    new PriceItem(1161, 20, 0.05m, 0m, 20m, 0.2m),
                    new PriceItem(1400, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(1411, 20, 0m, 0m, 15m, 0.05m),
                    new PriceItem(1481, 20, 0m, 0m, 0m, -0.15m),
                    new PriceItem(1500, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(1511, 20, 0m, 0m, 25m, 0.05m),
                    new PriceItem(1581, 20, 0m, 0m, 0m, -0.2m),
                    new PriceItem(1610, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(1612, 20, 0.003m, 0.005m, 40m, 0.12m),
                    new PriceItem(1613, 20, 0m, 0m, 20m, 0.05m),
                    new PriceItem(1615, 20, 0m, 0.01m, 20m, 0.05m),
                    new PriceItem(1621, 20, 0m, 0m, 10m, 0.05m),
                    new PriceItem(1622, 20, 0.003m, 0.005m, 40m, 0.12m),
                    new PriceItem(1631, 20, 0m, 0m, 10m, 0.05m),
                    new PriceItem(1632, 20, 0.003m, 0.011m, 40m, 0.12m),
                    new PriceItem(1641, 20, 0.003m, 0.007m, 40m, 0.12m),
                    new PriceItem(1642, 20, 0m, 0m, 20m, 0.05m),
                    new PriceItem(1661, 20, 0.003m, 0.021m, 40m, 0.12m),
                    new PriceItem(1662, 20, 0.003m, 0.021m, 40m, 0.12m),
                    new PriceItem(1671, 20, 0.003m, 0.038m, 40m, 0.12m),
                    new PriceItem(1672, 20, 0m, 0m, 20m, 0.05m),
                    new PriceItem(1673, 20, 0.003m, 0.006m, 40m, 0.12m),
                    new PriceItem(1699, 20, 0m, 0m, 20m, 0m),
                    new PriceItem(2100, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(2111, 20, 0m, 0m, 50m, 0.05m),
                    new PriceItem(2121, 20, 0.02m, 0m, 75m, 0.05m),
                    new PriceItem(2131, 20, 0m, 0m, 0m, 0.2m),
                    new PriceItem(2200, 20, 0m, 0m, 200m, 0.15m),
                    new PriceItem(2211, 20, 0m, 0m, 150m, 0.05m),
                    new PriceItem(2221, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(2300, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(2301, 20, 0m, 0.006m, 20m, 0.05m),
                    new PriceItem(2400, 20, 0m, 0.004m, 150m, 0.04m),
                    new PriceItem(2401, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(2500, 20, 0m, 0.004m, 150m, 0.04m),
                    new PriceItem(2501, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(3100, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(3111, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(3200, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(3211, 20, 0m, 0m, 0m, -0.05m),
                    new PriceItem(3300, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(3500, 20, 0m, 0m, 0m, 0m),
                    new PriceItem(3501, 20, 0m, 0.014m, 50m, 0.15m),
                    new PriceItem(3502, 20, 0m, 0.02m, 50m, 0.2m),
                    new PriceItem(3600, 20, 0m, 0m, 0m, 0m)
                };
                return list;
            }

            public class VariablePercentTier
            {
                public int ValueMin { get; set; }
                public int ValueMax { get; set; }
                public decimal PercentMin { get; set; }
                public decimal PercentMax { get; set; }
                public int ValueRange => ValueMax - ValueMin;
                public decimal PerecentRange => PercentMax - PercentMin;
                public VariablePercentTier(int valueMin, int valueMax, decimal percentMin, decimal percentMax)
                {
                    ValueMin = valueMin;
                    ValueMax = valueMax;
                    PercentMin = percentMin;
                    PercentMax = percentMax;
                }
            }
        }

        public enum ePricingOptionGroup
        {
            BasePrice = 10,
            Shape = 11,
            Print = 14,
            Dome = 15,
            PrintMaterial = 16,
            LayoutFor = 21,
            VariableData = 22,
            TransferTape = 23,
            Emboss = 24,
            FoilStamp = 25,
            DesignType = 31,
            PricingType = 32,
            OverageType = 33,
            OtherAdhesive = 35,
            MultipleDesigns = 51,
            PriceOverride = 71,
            Total = 90,
        }
        public enum ePricingOption
        {
            BasePrice = 1000,
            BaseTotalPrice = 1010,
            Circle = 1100,
            Oval = 1111,
            Square = 1121,
            Rectangle = 1131,
            Custom_Simple = 1141,
            Custom_Complex = 1151,
            Scripted = 1161,
            FullColor = 1400,
            FullColorAndWhite = 1411,
            NoPrint = 1481,
            Flexible = 1500,
            ExtraFlexible = 1511,
            NoDome = 1581,
            White_Permanent = 1610,
            White_ElephantSnot = 1612,
            White_Removable = 1613,
            White_NoAdhesive = 1615,
            BrightSilver_Permanent = 1621,
            BrightSilver_ElephantSnot = 1622,
            BrushedSilver_Permanent = 1631,
            BrushedSilver_ElephantSnot = 1632,
            Clear_ElephantSnot = 1641,
            Clear_PolyLiner_Permanent = 1642,
            Holographic_Shims_ElephantSnot = 1661,
            Holographic_NoShims_ElephantSnot = 1662,
            Reflective_ElephantSnot = 1671,
            MatteSilver_Bright_Permanent = 1672,
            MatteSilver_Dull_ElephantSnot = 1673,
            OtherPrintMaterial = 1699,
            Sheets = 2100,
            Sets = 2111,
            Singles = 2121,
            Rolls = 2131,
            NoVariableData = 2200,
            SkipsOkay = 2211,
            NoSkips = 2221,
            NoTransferTape = 2300,
            TransferTape = 2301,
            NoEmboss = 2400,
            Emboss = 2401,
            NoFoilStamp = 2500,
            FoilStamp = 2501,
            PrintAndDome = 3100,
            DomeOnly = 3111,
            Wholesale = 3200,
            Reseller = 3211,
            Exact = 3300,
            NoOtherAdhesive = 3500,
            Scrim = 3501,
            Foam = 3502,
            MultipleDesignVersions = 5101,
            NoOverrides = 7100,
            AdjustmentOverride = 7111,
            PricePerUnitOverride = 7112,
            TotalPriceOverride = 7113,
            SamplesOverride = 7180,
            NoCharge = 7190,
            BaseTotal = 9010,
            OptionsTotal = 9020,
            OtherTotal = 9030,
            StandardTotal = 9040,
            OverrideTotal = 9050,
            FinalTotal = 9002
        }
        public enum ePricingCalculationStep
        {
            Base = 10,
            Options = 20,
            Other = 30,
            Overrides = 50,
            Totals = 90,
        }
        public static PriceResponse Test()
        {


            List<Enum> enumList = new List<Enum>()
            {
                //eDt_dsn_shape.Circle,
                //eDt_printmaterial.White_Permanent,
                //eDt_dsn_print.FullColor,
                //eDt_dsn_dome.Flexible
            };
            var priceService = new PriceService();

            var request = new PriceRequest(1, 1, 100, enumList);
            request.AddOption(ePricingOption.NoOverrides);
            request.AddOption(ePricingOption.AdjustmentOverride, new PriceItem { PercentAdjustment = 0.5m, });
            //request.AddOption(ePricingOption.AdjustmentOverride, new PriceItem { LabelAdjustment = -0.01m, SquareInchAdjustment = 0.01m});
            //request.AddOption(ePricingOption.AdjustmentOverride, new PriceItem(-0.1m));
            //request.AddOption(ePricingOption.MultipleDesignVersions, new { value = 1 });
            var response = priceService.CalculatePrice(request);
            return response;

            //request.OtherOptions.Add("ManualAdjustment", new { Name = "PricePerUnitOverride", AdjustmentPerLabel = -.01 });
            //request.OtherOptions.Add("ManualAdjustment2", new { Name = "PricePerUnitOverride", AdjustmentPerLabel = -.01 });
            //request.OtherOptions.Add("TotalPriceOverride", new { value = 1 });
            //request.OtherOptions.Add("MutltipleDesignVersions", 6);
            //request.OtherOptions.Add("MutltipleDesignVersions2", 1);
            //request.OtherOptions.Add("MutltipleDesignVersions3", null);
            //request.OtherOptions.Add("enumtest", PriceRequest.ePricingOption.ManualAdjustments);
            //request.OtherOptions.Add("AWholePricingRequest", new PriceRequest(1, 1, 100, eDt_dsn_shape.Scripted, eDt_printmaterial.BrightSilver_ElephantSnot));

            //var requests = new List<PriceService.PriceRequest>()
            //{
            //    new PriceService.PriceRequest(1, 1, 10000000, eDt_dsn_shape.Scripted,
            //    eDt_printmaterial.BrightSilver_ElephantSnot,
            //    eDt_dsn_print.FullColorAndWhite,
            //    eDt_dsn_dome.Extra_Flexible),
            //    new PriceService.PriceRequest(1, 1, 10, enumList),
            //    new PriceService.PriceRequest(1, 1, 100, enumList),
            //    new PriceService.PriceRequest(1, 1, 1000, enumList),
            //    new PriceService.PriceRequest(1, 1, 10000, enumList),
            //    new PriceService.PriceRequest(1, 1, 100000, enumList),
            //    new PriceService.PriceRequest(1, 1, 1000000, enumList),
            //    new PriceService.PriceRequest(1, 1, 10000000, enumList),
            //};

            //foreach (var item in requests)
            //{
            //    var response = priceService.CalculatePrice(item);
            //    Console.WriteLine("Q:{0}, Price:{1}, Total:{2}",response.Request.Quantity, response.PricePerUnit, response.TotalPrice);
            //}
            //Console.ReadLine();


        }

    }

    public static class NumericHelpers
    {
        public static decimal SafeDivision(this decimal Numerator, decimal Denominator, int? roundTo = null)
        {
            if (Denominator == 0)
            {
                return 0;
            }

            decimal quotient = Numerator / Denominator;

            if (roundTo != null)
            {
                return Math.Round(quotient, roundTo.Value);
            }

            return quotient;
        }
        public static decimal SafeDivision(this decimal? Numerator, decimal? Denominator, int? roundTo = null)
        {
            if (Denominator == 0 || Numerator == null || Denominator == null)
            {
                return 0;
            }

            decimal quotient = Numerator.Value / Denominator.Value;

            if (roundTo != null)
            {
                return Math.Round(quotient, roundTo.Value);
            }

            return quotient;
        }
        public static decimal RoundUp(decimal value, int decimals)
        {
            var _decimals = (decimal)Math.Pow(10, decimals);
            return Math.Ceiling(value * _decimals) / _decimals;
        }
        public static decimal RoundUp3(decimal value) => Math.Ceiling(value * 1000) / 1000;
        public static decimal RoundUp2(decimal value) => Math.Ceiling(value * 100) / 100;

        public static int? MaxNullable(int? i1, int? i2)
        {
            int? result = null;
            if (i1 != null && i2 != null)
            {
                result = Math.Max(i1.Value, i2.Value);
            }
            else if (i1 != null)
            {
                result = i1;
            }
            else if (i2 != null)
            {
                result = i2;
            }
            return result;
        }
    }
}



