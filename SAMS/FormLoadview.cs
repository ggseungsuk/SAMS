using CefSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;

namespace SAMS
{
    public partial class FormLoadview : Form
    {
        //private System.Windows.Forms.WebBrowser webBrowser1 = new System.Windows.Forms.WebBrowser();

        private double lat = 0;
        private double lng = 0;

        public FormLoadview(double lat, double lng)
        {
            this.lat = lat;
            this.lng = lng;
            InitializeComponent();
        }
        public FormLoadview()
        {
            InitializeComponent();
        }
        public void MoveTo(double lat, double lng)
        {
            this.lat = lat;
            this.lng = lng;
            webBrowser1.EvaluateScriptAsync("moveTo(" + this.lng + ", " + this.lat + ");");
        }

        private void FormLoadview_Load(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("loadview lat : " + lat + ", lng: " + lng);
                string html = "<html>\n"
                    + "<body>\n"
                    + "<div id=\"map\" style=\"width:100%; height:100%; \"></div>\n"
                    + "<script type=\"text/javascript\" src=\"https://dapi.kakao.com/v2/maps/sdk.js?appkey=" + Properties.Settings.Default.kakaomap_appkey + "\"></script>\n"
                    + "<script>\n"
                    + "try{\n"
                    + "var roadviewContainer = document.getElementById(\"map\"); \n"
                    + "var roadview = new kakao.maps.Roadview(roadviewContainer); \n"
                    + "var roadviewClient = new kakao.maps.RoadviewClient(); \n"
                    + "moveTo(" + lng + ", " + lat + "); \n "
                    + "function moveTo(lat, lng) { \n"
                    + "    var loc = new kakao.maps.LatLng(lat, lng); \n"
                    + "	roadviewClient.getNearestPanoId(loc, 50, function(panoId) { \n"
                    + "		roadview.setPanoId(panoId, loc); \n"
                    + "	}); \n"
                    + "} \n"
                    + "}catch(e){ \n"
                    + "	alert(e.message); \n"
                    + "} \n"
                    + "</script> \n"
                    + "</body> \n"
                    + "</html> \n";

                webBrowser1.LoadHtml(html);
            }
            catch (Exception ex)
            {
                string tMsg = String.Format(" {0}", ex.Message);
                MessageBox.Show(tMsg);
            }
        }
    }
}
