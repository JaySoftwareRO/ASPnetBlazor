namespace ebayws
{
    public partial class PictureDetailsType
    {
        private string galleryURLField;

        [System.Xml.Serialization.XmlElementAttribute(DataType = "anyURI", Order = 9)]
        public string GalleryURL
        {
            get
            {
                return this.galleryURLField;
            }
            set
            {
                this.galleryURLField = value;
            }
        }
    }
}
