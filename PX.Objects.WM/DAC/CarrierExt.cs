using System;
using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.CS
{
    public class CarrierExt : PXCacheExtension<Carrier>
    {
        public abstract class returnLabel : PX.Data.IBqlField { }
        [PXDBBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Generate Return Label Automatically")]
        public virtual bool? ReturnLabel { get; set; }

        public abstract class isExternalShippingApplication : PX.Data.IBqlField { }
        [PXDBBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Use External Shipping Application")]
        public virtual bool? IsExternalShippingApplication { get; set; }

        public abstract class shippingApplicationType : PX.Data.IBqlField { }
        [PXDBString(IsFixed = true, IsUnicode = true)]
        [PXStringList(new string[] { "UPS", "FED" }, new string[] { "UPS WorldShip", "FedEx Ship Manager" })]
        [PXUIField(DisplayName = "Shipping Application")]
        public virtual string ShippingApplicationType { get; set; }
    }
}
