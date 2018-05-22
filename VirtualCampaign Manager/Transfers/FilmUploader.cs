using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Transfers
{
    public class FilmUploader : EventFireBase
    {
        private Production production;
        public string FilmSizeString;

        public FilmUploader(Production production)
        {
            this.production = production;
        }

        public void Upload()
        {
            production.UploadCounter = 0;

            if (production.IsPreview == false)
            {
                UploadPreviewDirectoryStandard();
            }
            else
            {
                UploadPreviewDirectoryProduct();
            }
        }

        private void UploadHashDirectory()
        {
            TransferPacket transferPacket = new TransferPacket(production, TransferType.UploadFilmDirectory);
            TransferManager transferManager = new TransferManager(transferPacket);
            transferManager.SuccessEvent += OnFilmUploadSuccess;
            transferManager.FailureEvent += OnFilmUploadFailure;
            transferManager.Transfer();
        }

        private void UploadPreviewDirectoryStandard()
        {
            TransferPacket transferPacket = new TransferPacket(production, TransferType.UploadFilmPreviewDirectory);
            TransferManager transferManager = new TransferManager(transferPacket);
            transferManager.SuccessEvent += OnFilmUploadSuccess;
            transferManager.FailureEvent += OnFilmUploadFailure;
            transferManager.Transfer();
        }

        private void UploadPreviewDirectoryProduct()
        {
            TransferPacket transferPacket = new TransferPacket(production, TransferType.UploadProductPreviewDirectory);
            TransferManager transferManager = new TransferManager(transferPacket);
            transferManager.SuccessEvent += OnFilmUploadSuccess;
            transferManager.FailureEvent += OnFilmUploadFailure;
            transferManager.Transfer();
        }

        private void OnFilmUploadSuccess(object sender, EventArgs ea)
        {
            TransferManager transferManager = sender as TransferManager;
            TransferPacket transferPacket = transferManager.Packet;
            transferManager.SuccessEvent -= OnFilmUploadSuccess;
            transferManager.FailureEvent -= OnFilmUploadFailure;
            transferManager = null;

            if (transferPacket.Type == TransferType.UploadFilmPreviewDirectory)
            {
                UploadHashDirectory();
            }
            else
            {
                FireSuccessEvent();
            }
        }

        private void OnFilmUploadFailure(object sender, EventArgs ea)
        {
            TransferManager transferManager = sender as TransferManager;
            TransferPacket transferPacket = transferManager.Packet;
            transferManager.SuccessEvent -= OnFilmUploadSuccess;
            transferManager.FailureEvent -= OnFilmUploadFailure;
            transferManager = null;

            FireFailureEvent(ProductionErrorStatus.PES_UPLOAD);
        }
    }
}
