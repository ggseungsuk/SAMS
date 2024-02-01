using AxTerraExplorerX;
using CefSharp;
using CefSharp.Structs;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web.UI;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Xml.Linq;
using TerraExplorerX;


namespace SAMS
{
    public partial class FormMain : Form
    {
        #region Ctor
        private SGWorld71 sgworld;
        private AxHost ThreeDWindow, InfoTree;
        private AxHost SideMap;

        private string mouseLMode = "none";

        private int numberLocation = 0;
        private int numberPolyline = 0;
        private int numberPolygon = 0;
        private int numberZone = 0;

        public static int zoneRadius = 500;
        public static bool showArrowCtrl = false;
        public static bool showSidemap = false;

        private string dataPath;

        private string tree;

        // 로드뷰 표시 고도 min 이하일때 open max 이상일때 close
        private int loadviewMinAltitute = 200;
        private int loadviewMaxAltitute = 500;
        // 지도 이동 시 로드뷰위치 재설정 거리
        private int loadviewMoveDistance = 100;

        // 로드뷰 표시 좌표 범위
        private double[] loadviewLatRange = { 33.230337, 38.01301 };
        private double[] loadviewLngRange = { 124.9763, 131.953462 };
        
        public FormMain()
        {
            InitializeComponent();

            ThreeDWindow = new AxTE3DWindow() { Dock = DockStyle.Fill };
            panMap.Controls.Add(ThreeDWindow);

            InfoTree = new AxTEInformationWindow() { Dock = DockStyle.Fill };
            panel2.Controls.Add(InfoTree);

            SideMap = new AxTENavigationMap() { Dock = DockStyle.Fill };
            panel3.Controls.Add(SideMap);

            //Data 폴더 확인 및 생성
            DirectoryInfo di = new DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists)
            {
                di.Create();
            }
            dataPath = di.FullName;
        }
        #endregion


        //---------------------------------------------------------------------
        //메인 폼 로딩
        //---------------------------------------------------------------------
        private void FormMain_Load(object sender, EventArgs e)
        {
            //컨트롤 위치 초기화
            guna2Panel4.Location = new System.Drawing.Point(6, 73);
            panMeasure.Location = new System.Drawing.Point(0, 84);

            //메뉴 비활성화
            btnSearch.Enabled = false;
            btnLayer.Enabled = false;
            btnMeasure.Enabled = false;
            btnSnashot.Enabled = false;
            btnHome.Enabled = false;
            btnLookaround.Enabled = false;
            btnZone.Enabled = false;
            btnSwitch3D.Enabled = false;
            lblSwitchValue.Enabled = false;
            label1.Enabled = false;
            btnSetting.Enabled = true;

            //c1RadialMenu1.ShowMenu(this, new Point(350, 350));
        }

        private void IconButton1_MouseHover(object sender, EventArgs e)
        {
            //iconButton1.ForeColor = Color.Black;
            //iconButton1.IconColor = Color.Black;
        }

        private void IconButton1_MouseLeave(object sender, EventArgs e)
        {
            //iconButton1.ForeColor = Color.DimGray;
            //iconButton1.IconColor = Color.DimGray;
        }

        private void IconButton6_Click(object sender, EventArgs e)
        {
            //검색결과 초기화
            guna2DataGridView1.Rows.Clear();

            //검색결과 컨트롤 맨 앞으로 보내기
            guna2Panel4.BringToFront();

            if (txtSearchKeyword.Text.Trim() == "시흥시청")
            {
                guna2Panel4.Visible = true;

                guna2DataGridView1.Rows.Add(null, "시흥시청", null, "경기도 시흥시 시청로 20 시흥시청", "", "");
                guna2DataGridView1.Rows.Add(null, "부평구청", null, "인천광역시 부평구 부평대로 168", "", "");
                guna2DataGridView1.Rows.Add(null, "부평구청역", null, "인천광역시", "", "");
                guna2DataGridView1.Rows.Add(null, "부평구청 공영주차장", null, "인천광역시 부평구 부평동 52-45", "", "");

                foreach (DataGridViewRow dgvr in guna2DataGridView1.Rows)
                {
                    DataGridViewImageCell imgCell = (DataGridViewImageCell)dgvr.Cells[2];
                    Image img = Properties.Resources.map_marker_alt_solid_16;
                    imgCell.Value = img;
                }
            }
        }

        private void IconButton12_MouseHover(object sender, EventArgs e)
        {
            toolTip1.ToolTipTitle = "위치";
            toolTip1.SetToolTip(btnCreateLocation, "현재 시점으로 위치를 저장합니다.");
        }

        private void IconButton7_MouseHover(object sender, EventArgs e)
        {
            toolTip1.ToolTipTitle = "벡터 데이터";
            toolTip1.SetToolTip(iconButton7, "벡터 형식의 데이터를 선택하여 불러옵니다.");
        }

        private void IconButton9_MouseHover(object sender, EventArgs e)
        {
            toolTip1.ToolTipTitle = "이미지 데이터";
            toolTip1.SetToolTip(iconButton9, "이미지 형식의 데이터를 선택하여 불러옵니다.");
        }

        // MyLayer > Layer 클릭 시
        private void IconButton7_Click(object sender, EventArgs e)
        {
            sgworld.Command.Execute(1013, 20);
            AddImportedMyLayer(Properties.Resources.object_group);
        }


        // MyLayer > 이미지 클릭 시
        private void IconButton9_Click(object sender, EventArgs e)
        {
            sgworld.Command.Execute(1014, 2);
            AddImportedMyLayer(Properties.Resources.images_regular);
        }

        // MyLayer > 산이미지 클릭 시
        private void IconButton8_Click(object sender, EventArgs e)
        {
            sgworld.Command.Execute(2110, 0);
            AddImportedMyLayer(Properties.Resources.mountain_solid);
        }
        // MyLayer > cute 클릭 시
        private void IconButton10_Click(object sender, EventArgs e)
        {
            sgworld.Command.Execute(1012, 25);
            AddImportedMyLayer(Properties.Resources.cubes_solid);
        }

        private String leastImportTreePathGroupID = "";

        // import를 이용한 tree 추가 시 tree목록중 최신 목록 리스트에 추가한다.
        private void AddImportedMyLayer(Image img)
        {
            ScanProjectTree("", ItemCode.ROOT);
            StringReader reader = new StringReader(tree);
            while (true)
            {
                string line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }

                string[] nodeMember = line.Split('|');

                if (nodeMember[4] == "1")
                {
                    // 최근에 추가한 groupId와 tree 리스트의 최신 groupID가 다르면 추가한다.
                    if (leastImportTreePathGroupID != nodeMember[2])
                    {
                        AddTreeNode(nodeMember[1], nodeMember[2], img);
                        leastImportTreePathGroupID = nodeMember[2];
                    }

                    break;
                }
            }
        }

        private void IconButton11_Click(object sender, EventArgs e)
        {
            sgworld.Command.Execute(1068, 0);
        }

        private void IconButton14_MouseHover(object sender, EventArgs e)
        {
            toolTip1.ToolTipTitle = "폴리라인";
            toolTip1.SetToolTip(btnCreatePolyline, "지도에서 폴리라인을 그립니다.");
        }

        private void IconButton15_MouseHover(object sender, EventArgs e)
        {
            toolTip1.ToolTipTitle = "폴리곤";
            toolTip1.SetToolTip(btnCreatePolygon, "지도에서 폴리곤을 그립니다.");
        }

        private void IconButton8_MouseHover(object sender, EventArgs e)
        {
            toolTip1.ToolTipTitle = "지형 모델";
            toolTip1.SetToolTip(iconButton8, "지형 모델을 선택하여 불러옵니다.");
        }

        private void IconButton10_MouseHover(object sender, EventArgs e)
        {
            toolTip1.ToolTipTitle = "3D 모델";
            toolTip1.SetToolTip(iconButton10, "3D 모델을 선택하여 불러옵니다.");
        }

        private void IconButton3_Click(object sender, EventArgs e)
        {
            panMeasure.BringToFront();

            if (panMeasure.Visible == true)
            {
                panMeasure.Visible = false;
            }
            else
            {
                panMeasure.Visible = true;
            }
        }

        private void IconButton16_Click(object sender, EventArgs e)
        {
            panMeasure.Visible = false;
            sgworld.Command.Execute(2356, 0);
            //sgworld.Command.Execute(2356, 1);
        }

        private void IconButton17_Click(object sender, EventArgs e)
        {
            panMeasure.Visible = false;
            sgworld.Command.Execute(2359, 0);
        }

        private bool OnMapLButtonClicked(int iflags, int ix, int iy)
        {
            if (mouseLMode == "none")
            {
                return false;
            }

            try
            {
                IWorldPointInfo71 iwpi = ConvertPixelToWorld(ix, iy);

                switch (mouseLMode)
                {
                    case "iconButton19":
                        //선택한 위치로 카메라를 이동
                        var cFlyToPos = iwpi.Position.Copy();
                        cFlyToPos.Pitch = 0;
                        if (cFlyToPos.Yaw >= 180)
                        {
                            cFlyToPos.Yaw = cFlyToPos.Yaw - 180;
                        }
                        else
                        {
                            cFlyToPos.Yaw = cFlyToPos.Yaw + 180;
                        }
                        sgworld.Navigate.FlyTo(cFlyToPos, ActionCode.AC_JUMP);

                        iconButton21.Visible = showArrowCtrl;
                        iconButton22.Visible = showArrowCtrl;
                        iconButton23.Visible = showArrowCtrl;
                        iconButton24.Visible = showArrowCtrl;

                        panel3.Visible = showSidemap;

                        break;
                    case "btnZone":
                        CreateCircleFromKML(zoneRadius, iwpi.Position.X, iwpi.Position.Y, "권역", "MyLayer\\Zone");

                        tabLayer.Visible = true;
                        tabLayer.SelectTab(1);

                        break;
                }
            }
            catch (Exception ex)
            {
                string tMsg = String.Format("OnMapLButtonClicked Exception: {0}", ex.Message);
                MessageBox.Show(tMsg);
            }

            mouseLMode = "none";
            return false;
        }

        private void CreateCircleFromKML(double radius, double x, double y, string Name, string groupPath)
        {
            //좌표계 변환(EPSG:4326 -> EPSG:5186)
            ICoordinateSystem71 cs4326 = sgworld.Terrain.CoordinateSystem;
            ICoordinateSystem71 cs5186 = sgworld.CoordServices.CreateCoordinateSystem("");
            cs5186.InitFromEPSG(5186);

            ICoord2D cs5186Pt = sgworld.CoordServices.Reproject(cs4326, cs5186, x, y);

            //원 둘레의 포인트 계산(32등분)            
            int divideValue = 32;
            double unitAngle = 360 / divideValue;
            string tempLineString = "";

            //교신거리
            string lineString = "";
            for (int i = 0; i < divideValue; i++)
            {
                double tempX = cs5186Pt.X + radius * Math.Sin(unitAngle * i * Math.PI / 180);
                double tempY = cs5186Pt.Y + radius * Math.Cos(unitAngle * i * Math.PI / 180);

                ICoord2D cs4326Pt = sgworld.CoordServices.Reproject(cs5186, cs4326, tempX, tempY);

                lineString = lineString + cs4326Pt.X.ToString() + "," + cs4326Pt.Y.ToString() + ",7 ";

                if (i == 0)
                {
                    tempLineString = lineString;
                }
            }
            lineString = lineString + tempLineString;

            //KML 파일 저장
            string filePath = @"C:\Temp\" + Name + ".kml";
            StreamWriter writer;
            writer = File.CreateText(filePath);
            writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            writer.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\" xmlns:sx=\"http://www.skylineglobe.com/kml/ext/7.03.000\" xmlns:gx=\"http://www.google.com/kml/ext/2.2\" xmlns:la=\"http://www.skylineglobe.com/layer/attributes\">");
            writer.WriteLine("  <Document>");
            writer.WriteLine("      <sx:Terrain>");
            writer.WriteLine("          <sx:RasterLayer><Link><href>/Imagery</href>");
            writer.WriteLine("                  <sx:layerName></sx:layerName>");
            writer.WriteLine("              </Link>");
            writer.WriteLine("              <sx:elevation>0</sx:elevation>");
            writer.WriteLine("          </sx:RasterLayer>");
            writer.WriteLine("          <sx:RasterLayer><Link><href>/Elevation</href>");
            writer.WriteLine("                  <sx:layerName></sx:layerName>");
            writer.WriteLine("              </Link>");
            writer.WriteLine("              <sx:elevation>1</sx:elevation>");
            writer.WriteLine("          </sx:RasterLayer>");
            writer.WriteLine("      </sx:Terrain>");
            writer.WriteLine("      <Style id=\"" + Name + "\">");
            writer.WriteLine("          <LineStyle><color>ff50d092</color>");
            writer.WriteLine("              <colorMode>normal</colorMode>");
            writer.WriteLine("              <width>2</width>");
            writer.WriteLine("          </LineStyle>");
            writer.WriteLine("          <PolyStyle><color>7f50d092</color>");
            writer.WriteLine("              <colorMode>normal</colorMode>");
            writer.WriteLine("              <fill>1</fill>");
            writer.WriteLine("          </PolyStyle>");
            writer.WriteLine("          <sx:order>0</sx:order>");
            writer.WriteLine("      </Style><Folder><name>range</name>");
            writer.WriteLine("          <open>1</open>");
            writer.WriteLine("          <Placemark id=\"0_50303915\"><name>" + Name + "</name>");
            writer.WriteLine("              <sx:tooltip>반경 " + radius + "m</sx:tooltip>");
            writer.WriteLine("              <styleUrl>#" + Name + "</styleUrl>");
            writer.WriteLine("              <Polygon>");
            writer.WriteLine("                  <tessellate>1</tessellate>");
            //writer.WriteLine("                  <altitudeMode>clampToGround</altitudeMode>");   //On terrain
            writer.WriteLine("                  <altitudeMode>relativeToGround</altitudeMode>");  //Relative to terrain
            writer.WriteLine("                  <outerBoundaryIs>");
            writer.WriteLine("                      <LinearRing>");
            writer.WriteLine("                          <coordinates>" + lineString + "</coordinates>");
            writer.WriteLine("                      </LinearRing>");
            writer.WriteLine("                  </outerBoundaryIs>");
            writer.WriteLine("              </Polygon>");
            writer.WriteLine("          </Placemark>");
            writer.WriteLine("          <Placemark id=\"0_50303915\"><name>반경 " + radius + "m</name>");
            writer.WriteLine("              <styleUrl>#label</styleUrl>");
            writer.WriteLine("              <Point>");
            writer.WriteLine("                  <coordinates>" + x.ToString() + "," + y.ToString() + ",7</coordinates>");
            writer.WriteLine("                  <altitudeMode>relativeToGround</altitudeMode>");
            writer.WriteLine("              </Point>");
            writer.WriteLine("          </Placemark>");
            writer.WriteLine("      </Folder>");
            writer.WriteLine("  </Document>");
            writer.WriteLine("</kml>");
            writer.Close();

            //그룹 생성 함수 호출
            string groupID = CreateGroup(groupPath, false);

            numberZone++;
            string subGroupID = sgworld.ProjectTree.CreateGroup("권역 #" + numberZone, groupID);

            //KML 파일 불러오기
            sgworld.ProjectTree.LoadKmlLayer(filePath, subGroupID);

            //내 레이어 목록 업데이트
            int rowIndex = dgvMyLayer.Rows.Add(null, null, "권역 #" + numberZone, "위치보기", "삭제", subGroupID);
            DataGridViewRow dgvr = dgvMyLayer.Rows[rowIndex];

            DataGridViewImageCell imgCell = (DataGridViewImageCell)dgvr.Cells[1];
            Image img = Properties.Resources.bullseye_solid_16;
            imgCell.Value = img;

            DataGridViewCheckBoxCell chkCell = (DataGridViewCheckBoxCell)dgvr.Cells[0];
            chkCell.Value = true;
            //chkCell.FlatStyle = FlatStyle.Flat;
            //chkCell.Style.ForeColor = Color.DimGray;
            //chkCell.ReadOnly = true;

            //position 객체 생성
            double dAltitude = 100.0;
            AltitudeTypeCode eAltitudeTypeCode = AltitudeTypeCode.ATC_ON_TERRAIN;
            double dYaw = 0.0;
            double dPitch = 0.0;
            double dRoll = 0.0;
            double dDistance = radius / Math.Tan(15 * Math.PI / 180);
            IPosition71 cPos = sgworld.Creator.CreatePosition(x, y, dAltitude, eAltitudeTypeCode, dYaw, dPitch, dRoll, dDistance);

            //해당 폴리곤으로 위치 이동
            var cFlyToPos = cPos.Copy();
            cFlyToPos.Pitch = -89.0; // Set camera to look downward on circle 
            sgworld.Navigate.FlyTo(cFlyToPos, ActionCode.AC_FLYTO);
        }

        private IWorldPointInfo71 ConvertPixelToWorld(int ix, int iy)
        {
            IWorldPointInfo71 iwpi = sgworld.Window.PixelToWorld(ix, iy, WorldPointType.WPT_DEFAULT);

            return iwpi;
        }

        private void IconButton13_Click(object sender, EventArgs e)
        {
            string treePath = "전체보기";
            string locID = sgworld.ProjectTree.FindItem(treePath);

            try
            {
                sgworld.Navigate.FlyTo(locID, ActionCode.AC_FLYTO);
            }
            catch (Exception e2) {
                Debug.WriteLine(e2.ToString() );
            }
            
        }

        private void Guna2DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            string treePath = "MyLayer\\시흥시청";
            string locID = sgworld.ProjectTree.FindItem(treePath);
            sgworld.Navigate.FlyTo(locID, ActionCode.AC_FLYTO);
        }

        private void IconButton18_Click(object sender, EventArgs e)
        {
            txtSearchKeyword.Clear();
            guna2Panel4.Visible = false;
            guna2DataGridView1.Rows.Clear();
        }

        //private void guna2DataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.ColumnIndex == 0)
        //    {
        //        string itemID = sgworld.ProjectTree.FindItem(guna2DataGridView3.Rows[e.RowIndex].Cells[1].Value.ToString());
        //        sgworld.Navigate.FlyTo(itemID, ActionCode.AC_FLYTO);
        //    }
        //}

        //private void guna2DataGridView4_CellContentClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    string itemID = sgworld.ProjectTree.FindItem(guna2DataGridView4.Rows[e.RowIndex].Cells[1].Value.ToString());
        //    IPresentation71 preObj = (IPresentation71)sgworld.ProjectTree.GetObject(itemID);
        //    preObj.Play(0);
        //}

        #region Menu
        //---------------------------------------------------------------------
        //메뉴 선택 - 검색
        //---------------------------------------------------------------------
        private void BtnSearch_Click(object sender, EventArgs e)
        {
            //패널 표시 전환
            if (guna2Panel3.Visible == false)
            {
                guna2Panel3.Visible = true;
            }
            else
            {
                guna2Panel3.Visible = false;
            }
        }

        //---------------------------------------------------------------------
        //메뉴 선택 - 레이어
        //---------------------------------------------------------------------
        private void BtnLayer_Click(object sender, EventArgs e)
        {
            tabLayer.BringToFront();

            if (tabLayer.Visible == false)
            {
                tabLayer.Visible = true;
            }
            else
            {
                tabLayer.Visible = false;
            }
        }

        //---------------------------------------------------------------------
        //메뉴 선택 - 측정
        //---------------------------------------------------------------------
        private void BtnMeasure_Click(object sender, EventArgs e)
        {
            panMeasure.BringToFront();

            if (panMeasure.Visible == true)
            {
                panMeasure.Visible = false;
            }
            else
            {
                panMeasure.Visible = true;
            }
        }

        //---------------------------------------------------------------------
        //메뉴 선택 - 캡쳐
        //---------------------------------------------------------------------
        private void BtnSnashot_Click(object sender, EventArgs e)
        {
            sgworld.Command.Execute(1068, 0);
        }

        //---------------------------------------------------------------------
        //메뉴 선택 - 조망권
        //---------------------------------------------------------------------
        private void BtnLookaround_Click(object sender, EventArgs e)
        {
            //메시지 바 출력
            ShowMessage("관측자 시점을 클릭하세요");

            mouseLMode = "iconButton19";
        }

        //---------------------------------------------------------------------
        //메뉴 선택 - 조망권
        //---------------------------------------------------------------------
        private void BtnZone_Click(object sender, EventArgs e)
        {
            //메시지 바 출력
            ShowMessage("권역 중심점을 클릭하세요");

            mouseLMode = "btnZone";
        }

        //---------------------------------------------------------------------
        //메뉴 선택 - 3D/2D 전환 버튼
        //---------------------------------------------------------------------
        private void BtnSwitch3D_CheckedChanged(object sender, EventArgs e)
        {
            if (btnSwitch3D.Checked == true)
            {
                lblSwitchValue.Text = "3D";
                lblSwitchValue.ForeColor = btnSwitch3D.CheckedState.FillColor;
                sgworld.Command.Execute(1052, 0);
            }
            else
            {
                lblSwitchValue.Text = "2D";
                lblSwitchValue.ForeColor = btnSwitch3D.UncheckedState.FillColor;
                sgworld.Command.Execute(1054, 0);
            }
        }

        //---------------------------------------------------------------------
        //메뉴 선택 - 설정
        //---------------------------------------------------------------------
        private void BtnSetting_Click(object sender, EventArgs e)
        {
            FormSetting frm = new FormSetting();
            frm.ShowDialog();
        }


        private void ToggleRoadView_CheckedChanged(object sender, EventArgs e)
        {

            if (toggleRoadView.Checked == true)
            {

                txtloadView.ForeColor = System.Drawing.Color.Black;
                OpenLoadMap();
            }
            else
            {
                txtloadView.ForeColor = System.Drawing.Color.Gray;
                CloseLoadMap();
            }

        }

        private TerraExplorerX.IPosition71 leastLoadviewpostion;

        //---------------------------------------------------------------------
        //로드뷰의 위치를 현재 지도시점으로 이동
        //---------------------------------------------------------------------
        private void MoveLoadViewPosition()
        {
            if (leastLoadviewpostion != null && Application.OpenForms.OfType<FormLoadview>().Any()) {
                FormLoadview formLoadview = Application.OpenForms.OfType<FormLoadview>().First();
                // 일정거리 이상 지도 이동 시 로드뷰 재위치 
                if (leastLoadviewpostion.DistanceTo(sgworld.Navigate.GetPosition()) >= loadviewMoveDistance)
                {
                    leastLoadviewpostion = sgworld.Navigate.GetPosition().Copy();
                    formLoadview.MoveTo(leastLoadviewpostion.X, leastLoadviewpostion.Y);
                }
            }
        }

        //---------------------------------------------------------------------
        //로드뷰 팝업을 띄운다
        //---------------------------------------------------------------------
        private void OpenLoadMap()
        {
            //if (btnSwitch3D.Checked == false) btnSwitch3D.Checked = true;
            if (Application.OpenForms.OfType<FormLoadview>().Any())
            {
                // 이전 loadview 위치와 현재 거리 측정
                Debug.WriteLine("openLoadMap >> distance2 : " + leastLoadviewpostion.DistanceTo(sgworld.Navigate.GetPosition()));
                FormLoadview formLoadview = Application.OpenForms.OfType<FormLoadview>().First();
                MoveLoadViewPosition();
            }
            else
            {
                FormLoadview f = new FormLoadview(sgworld.Navigate.GetPosition().X, sgworld.Navigate.GetPosition().Y);
                System.Drawing.Rectangle ScreenRectangle = Screen.PrimaryScreen.WorkingArea;
                f.Show();

                int xPos = ScreenRectangle.Width - f.Bounds.Width - 5;
                int yPos = ScreenRectangle.Height - f.Bounds.Height - 5;
                f.SetBounds(xPos, yPos, f.Size.Width, f.Size.Height, BoundsSpecified.Location);
                leastLoadviewpostion = sgworld.Navigate.GetPosition().Copy();
            }
        }

        //---------------------------------------------------------------------
        //로드뷰를 팝업을 닫는다
        //---------------------------------------------------------------------
        private void CloseLoadMap()
        {
            leastLoadviewpostion = null;
            //if (btnSwitch3D.Checked == true) btnSwitch3D.Checked = false;
            if (Application.OpenForms.OfType<FormLoadview>().Any())
            {
                Application.OpenForms.OfType<FormLoadview>().First().Close();
            }
        }

        //---------------------------------------------------------------------
        //메뉴 선택 - 종료
        //---------------------------------------------------------------------
        private void BtnExit_Click(object sender, EventArgs e)
        {

            DialogResult result = MessageBox.Show("SAMS를 종료하시겠습니까?", "확인", MessageBoxButtons.OKCancel);

            switch (result)
            {
                case DialogResult.OK:
                    {
                        try
                        {
                            //내 레이어 저장
                            SaveAsFly("MyLayer");
                        }
                        catch (Exception e2)
                        {
                            throw e2;
                        }
                        this.Close();
                        break;
                    }

                case DialogResult.Cancel:
                    {
                        break;
                    }
            }
        }
        #endregion


        private void TxtSearchKeyword_TextChanged(object sender, EventArgs e)
        {
            //검색어 입력시 텍스트 지움 버튼 활성화
            if (txtSearchKeyword.Text.Length > 0)
            {
                btnDeleteText.Visible = true;
            }
            else
            {
                btnDeleteText.Visible = false;
            }
        }


        #region TECommon
        //---------------------------------------------------------------------
        //FLY 프로젝트 열기
        //---------------------------------------------------------------------
        private void OpenProject(string tProjectUrl)
        {
            string tMsg = String.Empty;
            bool bIsAsync = false;
            string tUser = String.Empty;
            string tPassword = String.Empty;

            try
            {
                //var sgworld = new SGWorld71();
                sgworld = new SGWorld71();

                //이벤트 등록
                sgworld.OnLoadFinished += new _ISGWorld71Events_OnLoadFinishedEventHandler(Sgworld_OnLoadFinished);
                sgworld.OnLButtonClicked += new _ISGWorld71Events_OnLButtonClickedEventHandler(OnMapLButtonClicked);

                sgworld.OnRenderQualityChanged += new _ISGWorld71Events_OnRenderQualityChangedEventHandler(OnMapLOnRenderQualityChanged);

                sgworld.OnMouseWheel += new _ISGWorld71Events_OnMouseWheelEventHandler(OnMouseWheel);
                sgworld.OnLButtonDblClk += new _ISGWorld71Events_OnLButtonDblClkEventHandler(OnLButtonDblClk);

                sgworld.Project.Open(tProjectUrl, bIsAsync, tUser, tPassword);
                //MessageBox.Show("Opening project " + tProjectUrl + " in async mode");
            }
            catch (Exception ex)
            {
                tMsg = String.Format("OpenProjectButton_Click Exception: {0}", ex.Message);
                MessageBox.Show(tMsg);
            }
        }

        private bool OnMouseWheel(int Flags, short zDelta, int X, int Y)
        {
            //Debug.WriteLine("OnMouseWheel >> zDelta : " + zDelta);
            //AutoOpenCloseLoadView();
            return false;
        }
        private bool OnLButtonDblClk(int Flags, int X, int Y)
        {
            AutoOpenCloseLoadView();
            return false;
        }

        private void OnMapLOnRenderQualityChanged(int Quality)
        {
            Debug.WriteLine("OnMapLOnRenderQualityChanged >> " + Quality + "");
            // 화면이 다 그려지면 loadview 처리
            if (Quality == 100)
            {
                AutoOpenCloseLoadView();
            }
        }

        private void AutoOpenCloseLoadView() {
            var postion = sgworld.Navigate.GetPosition();

            Debug.WriteLine(postion.X + ", " + postion.Y);

            if (postion.X < loadviewLngRange[0] || postion.X > loadviewLngRange[1] ||
                postion.Y < loadviewLatRange[0] || postion.Y > loadviewLatRange[1]) {
                CloseLoadMap();
                return;
            }

            if (postion.Altitude < loadviewMinAltitute)
            {
                OpenLoadMap();
            }
            else if (postion.Altitude > loadviewMaxAltitute)
            {
                CloseLoadMap();
            } 
            else if (postion.Altitude < loadviewMaxAltitute) {

                MoveLoadViewPosition();
            }
        }

        /**

        private void OnLayerStreaming(string LayerGroupID,  bool bStreaming)
        {
            Debug.WriteLine("OnMeasurementAreaResult >> LayerGroupID : " + LayerGroupID + ", bStreaming : " + bStreaming);

        }

        private bool OnSGWorldMessage(string MessageID,       string SourceObjectID)
        {
            Debug.WriteLine("OnMeasurementAreaResult >> MessageID : " + MessageID + ", SourceObjectID : " + SourceObjectID);

            return false ;

        }

        private bool OnMeasurementAreaResult(string measurementAreaResult)
        {
            Debug.WriteLine("OnMeasurementAreaResult >> " + measurementAreaResult);

            return false;

        }

        private bool OnImportFeatureLayerProgress(int CurrPos, int Range)
        {
            Debug.WriteLine("OnImportFeatureLayerProgress >> " + CurrPos + "," + Range);

            return false;

        }


        private void  OnFeatureLayerSaved(string ObjectID)
        {
            Debug.WriteLine("OnFeatureLayerSaved >> " + ObjectID);

        }

        private void OnCommandValueChanged(int CommandID, object newVal)
        {

            Debug.WriteLine("OnCommandValueChanged >> " + CommandID + ", " + newVal);

        
        }



        private void OnEndDrawMeasurement(IGeometry pMeasurement)
        {
            Debug.WriteLine(pMeasurement.ToString());
        }

        */
        private void Sgworld_OnLoadFinished(bool bSuccess)
        {
            Debug.WriteLine("Sgworld_OnLoadFinished >> " + bSuccess + "");
            string tMsg = String.Empty;

            try
            {
                //메뉴 활성화
                btnSearch.Enabled = true;
                btnLayer.Enabled = true;
                btnMeasure.Enabled = true;
                btnSnashot.Enabled = true;
                btnHome.Enabled = true;
                btnLookaround.Enabled = true;
                btnZone.Enabled = true;
                btnSwitch3D.Enabled = true;
                btnSwitchSKY.Enabled = true;
                toggleRoadView.Enabled = true;  
                lblSwitchValue.Enabled = true;
                label1.Enabled = true;
                btnSetting.Enabled = true;

                //기본 레이어 불러오기
                tvwLayerDefault.Nodes.Clear();
                ScanProjectTree("", ItemCode.ROOT);

                //트리뷰에 노드 생성
                CreateTreeNode(tvwLayerDefault, tree);

                //MyLayer 그룹 불러오기
                string myLayerPath = dataPath + @"\MyLayer.fly";
                if (File.Exists(myLayerPath))
                {
                    sgworld.ProjectTree.LoadFlyLayer(dataPath + @"\MyLayer.fly", "");

                    //MyLayer 그룹 이동 & 그룹 삭제            
                    string groupID2 = sgworld.ProjectTree.FindItem("MyLayer");
                    string groupID = sgworld.ProjectTree.GetNextItem(groupID2, ItemCode.CHILD);
                    sgworld.ProjectTree.SetParent(groupID, "");
                    sgworld.ProjectTree.SetVisibility(groupID, false);
                    sgworld.ProjectTree.DeleteItem(groupID2);

                    //데이터 그리드뷰에 내 레이어 목록 불러오기
                    ScanProjectTree("MyLayer", ItemCode.CHILD);
                    LoadMyLayer(tree);
                }

                //이벤트 성공 확인 
                //MessageBox.Show("Received project loaded event");
            }
            catch (Exception ex)
            {
                tMsg = String.Format("OnProjectLoadFinished Exception: {0}", ex.Message);
                MessageBox.Show(tMsg);
            }
        }

        //---------------------------------------------------------------------
        //프로젝트 트리 그룹 생성
        //설명: 프로젝트 트리의 지정한 경로에, 해당 이름의 그룹을 생성
        //#1 treePath: 프로젝트의 트리의 경로 및 그룹 이름
        //#2 deleteDuplicates: 동일한 그룹이 이미 있을 경우 삭제 여부
        //---------------------------------------------------------------------
        private string CreateGroup(string treePath, bool deleteDuplicates)
        {
            //해당 경로에 동일한 그룹이 있는지 여부를 체크            
            string groupID = sgworld.ProjectTree.FindItem(treePath);

            if (groupID != "" && deleteDuplicates == false)
            {

            }
            else
            {
                if (deleteDuplicates == true)
                {
                    sgworld.ProjectTree.DeleteItem(groupID);
                }

                //그룹 생성
                groupID = "";
                string subTreePath = "";
                string[] nodeNames = treePath.Split('\\');
                foreach (string nodeName in nodeNames)
                {
                    subTreePath = subTreePath + nodeName;
                    string subGroupID = sgworld.ProjectTree.FindItem(subTreePath);
                    if (subGroupID == "")
                    {
                        groupID = sgworld.ProjectTree.CreateGroup(nodeName, groupID);
                    }
                    else
                    {
                        groupID = subGroupID;
                    }
                    subTreePath = subTreePath + "\\";
                }
            }

            return groupID;
        }

        //---------------------------------------------------------------------
        //메시지 바 출력(맵 하단)
        //---------------------------------------------------------------------
        private void ShowMessage(string strMessage)
        {
            sgworld.Window.ShowMessageBarText(strMessage);
        }

        //---------------------------------------------------------------------
        //프로젝트 트리 검색
        //---------------------------------------------------------------------
        private void ScanProjectTree(string startNodeName, ItemCode iCode)
        {
            try
            {
                //var root = sgworld.ProjectTree.GetNextItem(string.Empty, ItemCode.ROOT);

                string startNodeID = sgworld.ProjectTree.FindItem(startNodeName);
                var root = sgworld.ProjectTree.GetNextItem(startNodeID, iCode);

                if (sgworld.ProjectTree.GetItemName(root) == sgworld.ProjectTree.HiddenGroupName)
                {
                    root = sgworld.ProjectTree.GetNextItem(root, ItemCode.NEXT);
                }

                //var tree = BuildTreeRecursive(root, 1);
                tree = BuildTreeRecursive(root, 1);

                Debug.WriteLine(tree);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message);
            }
        }

        private string BuildTreeRecursive(string current, int indent)
        {
            var result = string.Empty;

            while (string.IsNullOrEmpty(current) == false)
            {
                var currentName = sgworld.ProjectTree.GetItemName(current);
                var parentID = sgworld.ProjectTree.GetNextItem(current, ItemCode.PARENT);   //부모 노드 체크
                var visibilityValue = sgworld.ProjectTree.GetVisibility(current);
                result += indent + "|" + currentName + "|" + current + "|" + parentID + "|" + visibilityValue + Environment.NewLine;
                if (sgworld.ProjectTree.IsGroup(current))
                {
                    var child = sgworld.ProjectTree.GetNextItem(current, ItemCode.CHILD);
                    result += BuildTreeRecursive(child, indent + 1);
                }

                current = sgworld.ProjectTree.GetNextItem(current, ItemCode.NEXT);
            }

            return result;
        }

        //---------------------------------------------------------------------
        //지정한 그룹을 fly 파일로 저장
        //---------------------------------------------------------------------
        private void SaveAsFly(string groupPath)
        {
            if (sgworld != null && sgworld.ProjectTree != null)
            {
                string groupID = sgworld.ProjectTree.FindItem(groupPath);
                if (groupID != null && groupID != "")
                {
                    string flyPath = sgworld.ProjectTree.SaveAsFly(groupPath, groupID);
                    System.IO.File.Copy(@flyPath, dataPath + @"\MyLayer.fly", true);
                }
            }
        }
        #endregion


        //---------------------------------------------------------------------
        //트리뷰에 노드 생성
        //---------------------------------------------------------------------
        private void CreateTreeNode(TreeView tView, string nodeList)
        {
            //노드 리스트 parsing
            StringReader reader = new StringReader(nodeList);
            while (true)
            {
                string line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }

                string[] nodeMember = line.Split('|');

                //트리 노드 생성
                TreeNode tn;
                if (nodeMember[3] == "")
                {
                    tn = tView.Nodes.Add(nodeMember[2], nodeMember[1]);
                }
                else
                {
                    tn = tView.Nodes.Find(nodeMember[3], true)[0].Nodes.Add(nodeMember[2], nodeMember[1]);
                }

                if (nodeMember[4] == "1")
                {
                    tn.Checked = true;
                }
                else
                {
                    tn.Checked = false;
                }

                //tView.ExpandAll();
            }
        }

        //---------------------------------------------------------------------
        //트리뷰 노트 더블클릭에 따른 해당 레이어 위치 이동
        //---------------------------------------------------------------------
        private void TvwLayerDefault_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Name != "")
            {
                if (!sgworld.ProjectTree.IsGroup(e.Node.Name))
                {
                    sgworld.Navigate.FlyTo(e.Node.Name, ActionCode.AC_FLYTO);
                }
            }
        }

        private void BtnOpenProject_DragDrop(object sender, DragEventArgs e)
        {
            var targetFile = "";

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var droppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (droppedFiles.Length != 1)
                {
                    MessageBox.Show("파일만 가능합니다.");
                    return;
                }

                targetFile = droppedFiles[0];

                if (File.Exists(targetFile) == false)
                {
                    MessageBox.Show("파일이 아닙니다.");
                    return;
                }

                FileInfo fi = new FileInfo(targetFile);

                if (fi.Extension.ToLower().CompareTo(".fly") != 0)
                {
                    MessageBox.Show("FLY 파일이 아닙니다.");
                    return;
                }
            }

            btnOpenProject.Visible = false;

            OpenProject(@targetFile);
        }

        private void BtnOpenProject_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void BtnOpenProject_Click(object sender, EventArgs e)
        {
            ofdFlyFile.DefaultExt = "fly";
            ofdFlyFile.Filter = "FLY 파일(*.fly)|*.fly";
            ofdFlyFile.ShowDialog();
        }

        private void OfdFlyFile_FileOk(object sender, CancelEventArgs e)
        {
            btnOpenProject.Visible = false;

            OpenProject(@ofdFlyFile.FileName);
        }

        //---------------------------------------------------------------------
        // 내 레이어 - 위치
        //---------------------------------------------------------------------
        private void BtnCreateLocation_Click(object sender, EventArgs e)
        {
            //그룹 생성 함수 호출
            string groupPath = "MyLayer\\Location";
            string groupID = CreateGroup(groupPath, false);

            numberLocation++;
            string name = "위치 #" + numberLocation;
            string subGroupID = sgworld.ProjectTree.CreateGroup(name, groupID);
            sgworld.ProjectTree.SelectItem(subGroupID);

            //Location 객체 생성
            ITerrainLocation71 objLocation = sgworld.Creator.CreateLocationHere(subGroupID, "");
            DataGridViewRow dgvr = AddTreeNode(name, subGroupID, Properties.Resources.map_marker_alt_solid_16);

            DataGridViewCheckBoxCell chkCell = (DataGridViewCheckBoxCell)dgvr.Cells[0];
            chkCell.Value = false;
            chkCell.FlatStyle = FlatStyle.Flat;
            chkCell.Style.ForeColor = Color.DimGray;
            chkCell.ReadOnly = true;
        }

        //---------------------------------------------------------------------
        // 내 레이어 - 폴리라인
        //---------------------------------------------------------------------
        private void BtnCreatePolyline_Click(object sender, EventArgs e)
        {
            //그룹 생성 함수 호출
            string groupPath = "MyLayer\\Polyline";
            string groupID = CreateGroup(groupPath, false);

            numberPolyline++;
            string name = "폴리라인 #" + numberPolyline;
            string subGroupID = sgworld.ProjectTree.CreateGroup(name, groupID);
            sgworld.ProjectTree.SelectItem(subGroupID);
            sgworld.Command.Execute(1012, 4);
            AddTreeNode(name, subGroupID, Properties.Resources.diagram_project_solid_16);
        }


        private DataGridViewRow AddTreeNode(string name, string subGroupID, Image img)
        {
            Debug.WriteLine("name : " + name + ", subGroupID" + subGroupID);
            int rowIndex = dgvMyLayer.Rows.Add(null, null, name, "위치보기", "삭제", subGroupID);
            DataGridViewRow dgvr = dgvMyLayer.Rows[rowIndex];

            DataGridViewImageCell imgCell = (DataGridViewImageCell)dgvr.Cells[1];
            imgCell.Value = img;

            DataGridViewCheckBoxCell chkCell = (DataGridViewCheckBoxCell)dgvr.Cells[0];
            chkCell.Value = true;
            //chkCell.FlatStyle = FlatStyle.Flat;
            //chkCell.Style.ForeColor = Color.DimGray;
            //chkCell.ReadOnly = true;
            return dgvr;
        }

        //---------------------------------------------------------------------
        // 내 레이어 - 폴리곤
        //---------------------------------------------------------------------
        private void BtnCreatePolygon_Click(object sender, EventArgs e)
        {
            //그룹 생성 함수 호출
            string groupPath = "MyLayer\\Polygon";
            string groupID = CreateGroup(groupPath, false);

            numberPolygon++;
            string name = "폴리곤 #" + numberPolygon;
            string subGroupID = sgworld.ProjectTree.CreateGroup(name, groupID);
            sgworld.ProjectTree.SelectItem(subGroupID);
            sgworld.Command.Execute(1012, 5);

            AddTreeNode(name, subGroupID, Properties.Resources.draw_polygon_solid_16);
        }

        private void DgvMyLayer_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 3)
            {
                sgworld.Navigate.FlyTo(dgvMyLayer.Rows[e.RowIndex].Cells[5].Value, ActionCode.AC_FLYTO);
            }
            else if (e.ColumnIndex == 4)
            {
                sgworld.ProjectTree.DeleteItem(dgvMyLayer.Rows[e.RowIndex].Cells[5].Value.ToString());
                dgvMyLayer.Rows.RemoveAt(e.RowIndex);
            }

            dgvMyLayer.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void LoadMyLayer(string nodeList)
        {
            //노드 리스트 parsing
            string layerType = "";

            StringReader reader = new StringReader(nodeList);
            while (true)
            {
                string line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }

                string[] nodeMember = line.Split('|');

                //레이어 타입별 부모 노드
                if (nodeMember[0] == "1")
                {
                    layerType = nodeMember[1];
                }

                //레이어 타입별 자식 노드(그룹)
                if (nodeMember[0] == "2")
                {
                    int rowIndex = dgvMyLayer.Rows.Add(null, null, nodeMember[1], "위치보기", "삭제", nodeMember[2]);
                    DataGridViewRow dgvr = dgvMyLayer.Rows[rowIndex];

                    DataGridViewCheckBoxCell chkCell = (DataGridViewCheckBoxCell)dgvr.Cells[0];
                    chkCell.Value = false;
                    //chkCell.FlatStyle = FlatStyle.Flat;
                    //chkCell.Style.ForeColor = Color.DimGray;
                    //chkCell.ReadOnly = true;

                    if (nodeMember[4] == "1")
                    {
                        chkCell.Value = true;
                    }
                    else
                    {
                        chkCell.Value = false;
                    }

                    Image img = null;
                    switch (layerType)
                    {
                        case "Location":
                            img = Properties.Resources.map_marker_alt_solid_16;
                            chkCell.FlatStyle = FlatStyle.Flat;
                            chkCell.Style.ForeColor = Color.DimGray;
                            chkCell.ReadOnly = true;
                            break;

                        case "Polyline":
                            img = Properties.Resources.diagram_project_solid_16;
                            break;

                        case "Polygon":
                            img = Properties.Resources.draw_polygon_solid_16;
                            break;

                        case "Zone":
                            img = Properties.Resources.bullseye_solid_16;
                            break;

                        default:
                            break;
                    }
                    DataGridViewImageCell imgCell = (DataGridViewImageCell)dgvr.Cells[1];
                    imgCell.Value = img;
                }

                if (nodeMember[0] == "3")
                {

                }
            }
        }

        //---------------------------------------------------------------------
        //내 레이어 체크(보기) 상태, 이름 변경
        //---------------------------------------------------------------------
        private void DgvMyLayer_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 0)
                {
                    try
                    {
                        sgworld.ProjectTree.SetVisibility(dgvMyLayer.Rows[e.RowIndex].Cells[5].Value.ToString(), Convert.ToBoolean(dgvMyLayer.Rows[e.RowIndex].Cells[0].Value));
                    }
                    catch (Exception ex) {
                        Debug.WriteLine(ex.ToString()); 
                    }
                }
                else if (e.ColumnIndex == 2)
                {
                    sgworld.ProjectTree.RenameGroup(dgvMyLayer.Rows[e.RowIndex].Cells[5].Value.ToString(), dgvMyLayer.Rows[e.RowIndex].Cells[2].Value.ToString());
                }
            }
        }



        private void BtnSwitchSKY_CheckedChanged(object sender, EventArgs e)
        {
            if (btnSwitchSKY.Checked == true)
            {
                lblSwitchValue.Text = "SKY";
                lblSwitchValue.ForeColor = btnSwitchSKY.CheckedState.FillColor;
                //    sgworld.Command.Execute(1052, 0);
            }
            else
            {
                lblSwitchValue.Text = "ROAD";
                lblSwitchValue.ForeColor = btnSwitchSKY.UncheckedState.FillColor;
                //    sgworld.Command.Execute(1054, 0);
            }
        }


        //---------------------------------------------------------------------
        //트리뷰 체크박스 선택/해제
        //---------------------------------------------------------------------
        private void TvwLayerDefault_AfterCheck(object sender, TreeViewEventArgs e)
        {
            //부모 노드 선택/해제에 따라 하위 노드 선택/해제 적용
            if (e.Node.Nodes.Count > 0)
            {
                foreach (TreeNode node in e.Node.Nodes)
                {
                    node.Checked = e.Node.Checked;
                }
            }

            //체크박스 선택/해제에 따라 해당 레이어의 표시/해제 적용 
            if (e.Node.Name != "")
            {
                sgworld.ProjectTree.SetVisibility(e.Node.Name, e.Node.Checked);
            }
        }
    }
}
