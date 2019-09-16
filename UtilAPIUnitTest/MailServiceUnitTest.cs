using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UtilAPIUnitTest.MailService;

namespace UtilAPIUnitTest
{
    [TestClass]
    public class MailServiceUnitTest
    {
        private static MailServiceClient _mailSvc;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _mailSvc = new MailServiceClient();
        }

        [TestMethod]
        public void MailService_Fedex_GetTrack()
        {
            //arrange
            string fedexKey = string.Empty;
            string fedexPassword = string.Empty;
            string fedexAccountNumber = string.Empty;
            string fedexMeterNumber = string.Empty;
            string fedexTrackingNumber = string.Empty;
            TrackRequest request = _mailSvc.CreateFedexTrackRequest(fedexKey, fedexPassword, fedexAccountNumber, fedexMeterNumber, fedexTrackingNumber);
            //act
            TrackReply reply = _mailSvc.GetFedexTrack(request);
            //assert
            foreach (CompletedTrackDetail detail in reply.completedTrackDetailsField)
            {
                foreach (TrackDetail trackDetail in detail.trackDetailsField)
                {
                    Address address = trackDetail.destinationAddressField;
                    //trackDetail.actualDeliveryAddressField;

                    string status = trackDetail.statusDetailField != null ? trackDetail.statusDetailField.descriptionField : "";
                    DateTime actualDeliveryTime;
                    DateTime estimatedDeliveryTime;
                    DateTime pickupTime;
                    if (trackDetail.estimatedPickupTimestampFieldSpecified)
                        pickupTime = trackDetail.estimatedPickupTimestampField;
                    if (trackDetail.actualDeliveryTimestampFieldSpecified)
                        actualDeliveryTime = trackDetail.actualDeliveryTimestampField;
                    if (trackDetail.estimatedDeliveryTimestampFieldSpecified)
                        estimatedDeliveryTime = trackDetail.estimatedDeliveryTimestampField;
                }
            }
        }

        [TestMethod]
        public void MailService_UPS_GetTrack()
        {
            //arrange
            string UPSUser = string.Empty;
            string UPSPassword = string.Empty;
            string UPSAccessKey = string.Empty;
            string UPStrackingNumber = string.Empty;

            string arrivalDate = string.Empty;
            string arrivalTime = string.Empty;

            //act
            TrackRequest1 UPSrequest = _mailSvc.CreateUPSTrackRequest(UPStrackingNumber);
            TrackResponse UPSresponse = _mailSvc.GetUPSTrack(UPSUser, UPSPassword, UPSAccessKey, UPSrequest);

            //assert
            foreach (ShipmentType shipment in UPSresponse.shipmentField)
            {
                string status = shipment.currentStatusField != null ? shipment.currentStatusField.descriptionField : "";
                if (shipment.destinationPortDetailField != null)
                {
                    DateTimeType arrival = shipment.destinationPortDetailField.estimatedArrivalField;

                    Assert.IsNotNull(arrival);
                    Assert.AreEqual(arrivalDate, arrival.dateField);
                    Assert.AreEqual(arrivalTime, arrival.timeField);
                }
            }
        }
    }
}
