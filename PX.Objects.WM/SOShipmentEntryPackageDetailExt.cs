using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.SO
{
    //TODO: Move to SOShipmentEntry graph when we integrate into Acumatica code base
    public class SOShipmentEntryPackageDetailExt : PXGraphExtension<SOShipmentEntry>
    {
        public PXSelect<SOPackageDetailSplit,
            Where<SOPackageDetailSplit.shipmentNbr, Equal<Current<SOPackageDetail.shipmentNbr>>,
            And<SOPackageDetailSplit.lineNbr, Equal<Current<SOPackageDetail.lineNbr>>>>> PackageDetailSplit;

        protected void SOShipment_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            //TODO: Update automation rules and remove this code - this is only temporary while this code is being executed as customization
            PackageDetailSplit.AllowDelete = Base.Packages.AllowDelete;
            PackageDetailSplit.AllowInsert = Base.Packages.AllowInsert;
            PackageDetailSplit.AllowSelect = Base.Packages.AllowSelect;
            PackageDetailSplit.AllowUpdate = Base.Packages.AllowUpdate;
        }
        
        protected virtual void SOPackageDetailSplit_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
        {
            UpdateParentShipmentLine(sender, (SOPackageDetailSplit) e.Row, null);
        }

        protected virtual void SOPackageDetailSplit_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
        {
            UpdateParentShipmentLine(sender, (SOPackageDetailSplit) e.Row, (SOPackageDetailSplit) e.OldRow);
        }

        protected virtual void SOPackageDetailSplit_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
        {
            UpdateParentShipmentLine(sender, null, (SOPackageDetailSplit)e.Row);
        }

        protected void UpdateParentShipmentLine(PXCache sender, SOPackageDetailSplit row, SOPackageDetailSplit oldRow)
        {
            if (row != null && oldRow != null && row.ShipmentLineNbr == oldRow.ShipmentLineNbr)
            {
                var shipmentLine = (SOShipLine) PXParentAttribute.SelectParent(sender, row, typeof(SOShipLine));
                if(shipmentLine != null)
                { 
                    UpdateShipmentLine(sender, shipmentLine, row, row.Qty.GetValueOrDefault() - oldRow.Qty.GetValueOrDefault());
                }
            }
            else
            {
                if(row != null)
                {
                    var shipmentLine = (SOShipLine) PXParentAttribute.SelectParent(sender, row, typeof(SOShipLine));
                    if (shipmentLine != null)
                    {
                        UpdateShipmentLine(sender, shipmentLine, row, row.Qty.GetValueOrDefault());
                    }
                }

                if(oldRow != null)
                {
                    var shipmentLine = (SOShipLine) PXParentAttribute.SelectParent(sender, oldRow, typeof(SOShipLine));
                    if (shipmentLine != null)
                    {
                        UpdateShipmentLine(sender, shipmentLine, row, -oldRow.Qty.GetValueOrDefault());
                    }
                }
            }
        }

        protected void UpdateShipmentLine(PXCache sender, SOShipLine shipmentLine, SOPackageDetailSplit packageDetailSplit, decimal adjustment)
        {
            if (adjustment != 0)
            {
                var originalShipmentLine = PXCache<SOShipLine>.CreateCopy(shipmentLine);

                var shipmentLineExt = PXCache<SOShipLine>.GetExtension<SOShipLineExt>(shipmentLine);
                decimal? originalPackagedQty = shipmentLineExt.PackedQty;

                shipmentLineExt.PackedQty = shipmentLineExt.PackedQty.GetValueOrDefault() + adjustment;

                //Pattern used in LSSelect::UpdateParent - is this proper?
                if (sender.Graph.Caches[typeof(SOShipLine)].GetStatus(shipmentLine) == PXEntryStatus.Notchanged)
                {
                    sender.Graph.Caches[typeof(SOShipLine)].SetStatus(shipmentLine, PXEntryStatus.Updated);
                }

                sender.Graph.Caches[typeof(SOShipLine)].RaiseFieldUpdated<SOShipLineExt.packedQty>(shipmentLine, originalPackagedQty);
                sender.Graph.Caches[typeof(SOShipLine)].RaiseRowUpdated(shipmentLine, originalShipmentLine);

                if(shipmentLineExt.PackedQty > shipmentLine.ShippedQty)
                {
                    sender.RaiseExceptionHandling<SOPackageDetailSplit.qty>(packageDetailSplit, packageDetailSplit.Qty, new PXSetPropertyException(PX.Objects.WM.Messages.QuantityPackedExceedsShippedQuantityForLine));
                }
            }
        }

        protected void SOPackageDetailSplit_ShipmentLineNbr_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            var row = (SOPackageDetailSplit)e.Row;
            if(row != null)
            { 
                var shipmentLine = (SOShipLine)PXParentAttribute.SelectParent(sender, row, typeof(SOShipLine));
                var shipmentLineExt = PXCache<SOShipLine>.GetExtension<SOShipLineExt>(shipmentLine);
                row.InventoryID = shipmentLine?.InventoryID;
                row.UOM = shipmentLine?.UOM;
                row.Qty = shipmentLine?.ShippedQty - shipmentLineExt?.PackedQty;
            }
        }

        public delegate void ShipPackagesDelegate(SOShipment shiporder);
        [PXOverride]
        public virtual void ShipPackages(SOShipment shiporder, ShipPackagesDelegate baseMethod)
        {
            ValidatePackagedQuantities(shiporder);
            GenerateReturnLabels(shiporder);
            baseMethod(shiporder);
        }
        
        protected virtual void ValidatePackagedQuantities(SOShipment shiporder)
        {
            Base.Document.Current = Base.Document.Search<SOShipment.shipmentNbr>(shiporder.ShipmentNbr);

            if (Base.Document.Current.ShipmentType == SOShipmentType.Issue)
            {
                foreach (SOShipLine line in Base.Transactions.Select())
                {
                    if (line.LineType == SOLineType.Inventory)
                    {
                        SOShipLineExt ext = PXCache<SOShipLine>.GetExtension<SOShipLineExt>(line);
                        if (line.BaseShippedQty != ext.BasePackedQty)
                        {
                            InventoryItem item = PXSelectorAttribute.Select<SOShipLine.inventoryID>(Base.Transactions.Cache, line) as InventoryItem;
                            throw new PXException(PX.Objects.WM.Messages.ShipmentLineQuantityNotPacked, item?.InventoryCD.Trim());
                        }
                    }
                }
            }
        }

        protected virtual void GenerateReturnLabels(SOShipment shiporder)
        {
            Carrier carrier = PXSelect<Carrier, Where<Carrier.carrierID, Equal<Required<SOShipment.shipVia>>>>.Select(Base, shiporder.ShipVia);
            if (carrier != null && carrier.IsExternal == true && shiporder.ShippedViaCarrier != true)
            {
                //Automatically print return label if enabled for selected ship via
                var ext = PXCache<Carrier>.GetExtension<CS.CarrierExt>(carrier);
                if (ext.ReturnLabel == true)
                {
                    Base.GetReturnLabels(shiporder);
                }
            }
        }
    }


}
