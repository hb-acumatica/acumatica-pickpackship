using System;
using PX.Data;

namespace PX.Objects.CS
{
    //TODO: Move to CarrierMaint graph when we integrate into Acumatica code base
    public class CSCarrierMaintExt : PXGraphExtension<CarrierMaint>
    {
        protected virtual void Carrier_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            Carrier row = e.Row as Carrier;
            if (row != null)
            {
                // Shipping application integration is mutually exclusive with external carrier plug-in. You can't use both at the same time.
                CarrierExt rowExt = sender.GetExtension<CarrierExt>(row);
                PXUIFieldAttribute.SetEnabled<Carrier.isExternal>(sender, row, rowExt.IsExternalShippingApplication == false);
                PXUIFieldAttribute.SetVisible<CarrierExt.returnLabel>(sender, row, row.IsExternal == true);
                PXUIFieldAttribute.SetEnabled<CarrierExt.isExternalShippingApplication>(sender, row, row.IsExternal == false);
                PXUIFieldAttribute.SetEnabled<CarrierExt.shippingApplicationType>(sender, row, row.IsExternal == false);
                PXUIFieldAttribute.SetEnabled<CarrierExt.shippingApplicationType>(sender, row, rowExt.IsExternalShippingApplication == true);

            }
        }

        protected virtual void Carrier_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
        {
            Carrier row = (Carrier)e.Row;
            CarrierExt rowExt = sender.GetExtension<CarrierExt>(row);

            if (rowExt.IsExternalShippingApplication == true && String.IsNullOrEmpty(rowExt.ShippingApplicationType))
            {
                throw new PXRowPersistingException(typeof(CarrierExt.shippingApplicationType).Name, null, ErrorMessages.FieldIsEmpty, typeof(CarrierExt.shippingApplicationType).Name);
            }
        }
    }
}
