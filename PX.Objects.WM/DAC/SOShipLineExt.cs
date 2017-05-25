using System;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.IN;

namespace PX.Objects.SO
{
    public class SOShipLineExt : PXCacheExtension<SOShipLine>
    {
        public abstract class packedQty : PX.Data.IBqlField { }
        [PXDBQuantity(typeof(SOShipLine.uOM), typeof(SOShipLineExt.basePackedQty))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Packed Qty.", Enabled = false)]
        public virtual Decimal? PackedQty { get; set; }

        public abstract class basePackedQty : PX.Data.IBqlField { }
        [PXDBDecimal(6)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual Decimal? BasePackedQty { get; set; }
    }
}
