using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Helpers;

namespace VirtualCampaign_Manager.Transfers
{
    public class FilmUploader : EventFireBase
    {
        private Production production;
        public string FilmSizeString;
        private TransferManager transferManager;

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
            transferPacket.SuccessEvent += OnFilmUploadSuccess;
            transferPacket.FailureEvent += OnFilmUploadFailure;

            transferManager = new TransferManager();
            transferManager.Transfer(transferPacket);
        }

        private void UploadPreviewDirectoryStandard()
        {
            TransferPacket transferPacket = new TransferPacket(production, TransferType.UploadFilmPreviewDirectory);
            transferPacket.SuccessEvent += OnFilmUploadSuccess;
            transferPacket.FailureEvent += OnFilmUploadFailure;

            transferManager = new TransferManager();
            transferManager.Transfer(transferPacket);
        }

        private void UploadPreviewDirectoryProduct()
        {
            TransferPacket transferPacket = new TransferPacket(production, TransferType.UploadProductPreviewDirectory);
            transferPacket.SuccessEvent += OnFilmUploadSuccess;
            transferPacket.FailureEvent += OnFilmUploadFailure;

            transferManager = new TransferManager();
            transferManager.Transfer(transferPacket);

            if (production.IsPreview)
            {
                string sourcePath = ProductionPathHelper.GetLocalProductPreviewProductionDirectory(production);
                string destinationPath = ProductionPathHelper.GetProductDirectory(production.JobList[0].ProductID);
                foreach (string newPath in Directory.GetFiles(sourcePath))
                    File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
            }
        }

        private void OnFilmUploadSuccess(object sender, EventArgs ea)
        {
            TransferPacket transferPacket = sender as TransferPacket;
            transferPacket.SuccessEvent -= OnFilmUploadSuccess;
            transferPacket.FailureEvent -= OnFilmUploadFailure;

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
            TransferPacket transferPacket = sender as TransferPacket;
            transferPacket.SuccessEvent -= OnFilmUploadSuccess;
            transferPacket.FailureEvent -= OnFilmUploadFailure;

            FireFailureEvent(ProductionErrorStatus.PES_UPLOAD);
        }
    }
}
