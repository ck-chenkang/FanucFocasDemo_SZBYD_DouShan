using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FanucFocasDemo
{

    public partial class Form1 : Form
    {
        public short ret;//返回状态
        private int timeout = 5;//5秒如果没响应，就认为不在线
        ushort cncHandle = 0;//连接成功后会返回句柄，使用这个句柄访问函数，注意本句柄不能跨线程
        public float x, y, z;//绝对坐标
        public float mx, my, mz;//相对坐标
        public int FeedRate;//进给率
        public string Speed = string.Empty;//转速
        public int Tool_no;//刀具号
        public short run;//运行标志
        public short Alarm;//报警状态
        public int SeqNum;//行号
        public short Mainpg;//主程序号
        public short Currentpg;//当前程序号
        //暂时不用
        public short actf_unit, actf_dec;
        public int actf_data;
        public short acts_unit, acts_dec;
        public int acts_data;
        //系统信息
        public string CNCModel;//工作模式
        public string MaxAxis;//最大主轴
        public string CNCType;//机床类型
        public string MTType;//M/T类型
        public string SerialNumber;//系列号
        public string Version;//版本号
        public string Axis;//控制轴数
        public string AlarmState;//报警状态
        public string AlarmMessage;//报警信息
        public string SpindleLoad;//主轴负载
        public string SpindleOverRide;//主轴倍率

        private void button2_Click(object sender, EventArgs e)
        {
            if (opdCNC.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = opdCNC.FileName;

                StreamReader sr = new StreamReader(txtFilePath.Text, Encoding.Default);
                StringBuilder sbtxt = new StringBuilder();
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    sbtxt.Append(line);
                    sbtxt.Append("\n");
                }
                sr.Close();
                paramFromFile = sbtxt.ToString();
                MessageBox.Show(paramFromFile);
            }
        }

        public string FeedOverRide;//进给倍率

        private void btnFileDown_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(paramFromFile))
            {
                int len;
                int n;
                short ret;

                short ret1;
                short num = 999;
                //删除程序文件（删除的文件不能是主程序文件）
                ret1 = Focas1.cnc_delete(cncHandle, num);
                switch (ret1)
                {
                    case Focas1.EW_OK:
                        MessageBox.Show(string.Format("PROGRAM O%d has been deleted.\n", num));
                        break;
                    case (short)Focas1.focas_ret.EW_DATA:
                        MessageBox.Show(string.Format("PROGRAM O%d doesn't exist.\n", num));
                        break;
                    case (short)Focas1.focas_ret.EW_PROT:
                        MessageBox.Show(string.Format("PROTECTED.\n"));
                        break;
                    case (short)Focas1.focas_ret.EW_BUSY:
                        MessageBox.Show(string.Format("REJECTED.\n"));
                        break;
                }


                ret = Focas1.cnc_dwnstart4(cncHandle, 0, "//CNC_MEM/USER/LIBRARY");
                if (ret == -1)
                {
                    ret = Focas1.cnc_dwnend4(cncHandle);
                    return;
                }
                if (ret != Focas1.EW_OK)
                {
                    MessageBox.Show("下发参数失败");
                    return;
                }

                len = paramFromFile.Length;
                while (len > 0)
                {
                    n = len;
                    ret = Focas1.cnc_download4(cncHandle, ref n, paramFromFile);
                    if (ret == (short)Focas1.focas_ret.EW_BUFFER)
                    {
                        continue;
                    }
                    if (ret == Focas1.EW_OK)
                    {
                        paramFromFile += n;
                        len -= n;
                    }
                    if (ret != Focas1.EW_OK)
                    {
                        break;
                    }
                }
                ret = Focas1.cnc_dwnend4(cncHandle);

                MessageBox.Show("下发完成!");

            }
            else
            {
                MessageBox.Show("请选择参数文件");
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            //file address
            //open 
            StringBuilder sbParamFile = new StringBuilder();

            int BUFSIZE = 1280;
            char[] buf = new char[BUFSIZE + 1];
            short ret;
            int len;
            //ret = Focas1.cnc_upstart3(cncHandle, 0, 1, 1);
            ret = Focas1.cnc_upstart4(cncHandle, 0, "O0001");
            if (ret != Focas1.EW_OK)
            {
                MessageBox.Show("读取参数文件失败");
                return;
            }
            do
            {
                len = BUFSIZE;
                //ret = Focas1.cnc_upload3(cncHandle, ref len, buf);
                ret = Focas1.cnc_upload4(cncHandle, ref len, buf);
                if (ret == (short)Focas1.focas_ret.EW_BUFFER)
                {
                    continue;
                }
                if (ret == Focas1.EW_OK)
                {
                    buf[len] = '\0';
                    for (int i = 0; i < len; i++)
                    {
                        sbParamFile.Append(buf[i]);
                    }
                    saveFile(sbParamFile.ToString());
                }
                if (buf[len - 1] == '%')
                {
                    break;
                }
            } while (ret == Focas1.EW_OK);
            _ = Focas1.cnc_upend4(cncHandle);
            MessageBox.Show(sbParamFile.ToString());
        }
        private void saveFile(string file)
        {
            FileStream fs = new FileStream("X:\\O0999.NC", FileMode.Create);
            byte[] data = Encoding.Default.GetBytes(file);
            fs.Write(data, 0, data.Length);
            fs.Flush();
            fs.Close();
        }

        private void btn_sltmain_Click(object sender, EventArgs e)
        {
            try
            {
                //
                object mainnStr = "//CNC_MEM/USER/PATH1/O1";

                var ret = Focas1.cnc_pdf_slctmain(cncHandle, mainnStr);
                if (ret == Focas1.EW_OK)
                {
                    MessageBox.Show("Sucess");
                }
                else
                {

                    MessageBox.Show($"{ret}");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_Opendoor_Click(object sender, EventArgs e)
        {
            try
            {
                //R0012.5
                var ret = cncDoorAction(32);
                if (ret == Focas1.EW_OK)
                {
                    // MessageBox.Show("sucess");
                    //监听信号
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    bool isOpen = false;
                    while (!isOpen)
                    {
                        var result = ReadCnc(33, 33);
                        if (result != null)
                        {
                            if (result.cdata[0] == 32 || result.cdata[0] == 64)
                            {
                                MessageBox.Show($"rsult:{result.cdata[0]}");
                                break;
                            }
                        }
                        if (sw.ElapsedMilliseconds >= 5 * 10000)
                        {
                            MessageBox.Show($"超时ElapsedMilliseconds={sw.ElapsedMilliseconds}");
                            break;
                        }
                    }

                }
                else
                {
                    MessageBox.Show(ret.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private Focas1.focas_ret cncDoorAction(byte adr)
        {
            Focas1.IODBPMC0 pmc_data = new Focas1.IODBPMC0();
            pmc_data.cdata = new byte[8];
            pmc_data.type_a = 5;
            pmc_data.type_d = 0;

            pmc_data.datano_s = 12;
            pmc_data.datano_e = 12;
            pmc_data.cdata[0] = adr;

            return (Focas1.focas_ret)Focas1.pmc_wrpmcrng(cncHandle, 9, pmc_data);
        }

        private void btn_CloseDoor_Click(object sender, EventArgs e)
        {
            try
            {
                //R0012.5
                var ret = cncDoorAction(64);
                if (ret == Focas1.EW_OK)
                {
                    // MessageBox.Show("sucess");
                    //监听信号
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    bool isOpen = false;
                    while (!isOpen)
                    {
                        var result = ReadCnc(33, 33);
                        if (result != null)
                        {
                            if (result.cdata[0] == 32 || result.cdata[0] == 64)
                            {
                                MessageBox.Show($"rsult:{result.cdata[0]}");
                                break;
                            }
                        }
                        if (sw.ElapsedMilliseconds >= 5 * 10000)
                        {
                            MessageBox.Show($"超时ElapsedMilliseconds={sw.ElapsedMilliseconds}");
                            break;
                        }
                    }

                }
                else
                {
                    MessageBox.Show(ret.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        public int SpindleSpeed;//主轴转速

        /// <summary>
        /// 执行主程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                //判断模式和状态
                var state = ReadCnc(4001, 4001);
                if (state != null)
                {
                    //状态1的时候才可以执行
                    if (state.cdata[0] == 1)
                    {
                        //adr
                        bool isOk = false;
                        for (int i = 0; i < 3; i++)
                        {

                            Focas1.IODBPMC0 pmc_data = new Focas1.IODBPMC0();
                            pmc_data.cdata = new byte[8];
                            pmc_data.type_a = 5;
                            pmc_data.type_d = 0;

                            pmc_data.datano_s = 1300;
                            pmc_data.datano_e = 1300;

                            //pmc_data.cdata[0] = 1;
                            if (i == 1)
                            {
                                Stopwatch sw = Stopwatch.StartNew();
                                sw.Start();
                                while (true)
                                {
                                    pmc_data.cdata[0] = 1;
                                    var ret = (Focas1.focas_ret)Focas1.pmc_wrpmcrng(cncHandle, 9, pmc_data);
                                    if (ret == Focas1.focas_ret.EW_OK)
                                    {
                                        isOk = true;
                                    }
                                    if (sw.ElapsedMilliseconds >= 500)
                                    {
                                        sw.Stop();
                                        break;
                                    }
                                }
                                //Task.Delay(500).ConfigureAwait(false);
                            }
                            else
                            {
                                pmc_data.cdata[0] = 0;
                                var ret = (Focas1.focas_ret)Focas1.pmc_wrpmcrng(cncHandle, 9, pmc_data);
                                if (ret == Focas1.focas_ret.EW_OK)
                                {
                                    isOk = true;
                                }
                            }


                        }
                        if (isOk)
                        {

                            MessageBox.Show("sucess");

                        }
                    }
                    else
                    {
                        MessageBox.Show($"4001状态={state.cdata[0]},状态错误");
                    }
                }
                else
                {
                    MessageBox.Show("读取4001状态失败");
                }
                Focas1.cnc_freelibhndl(cncHandle);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_startNew_Click(object sender, EventArgs e)
        {
            var ret = Focas1.cnc_start(cncHandle);
            if (ret == 0)
            {
                MessageBox.Show("sucess");
            }
            //
            //bool isOk = false;
            //for (int i = 0; i < 3; i++)
            //{

            //    Focas1.IODBPMC0 pmc_data = new Focas1.IODBPMC0
            //    {
            //        cdata = new byte[8],
            //        type_a = 3,
            //        type_d = 0,
            //        datano_s = 97,
            //        datano_e = 97
            //    };

            //    //pmc_data.cdata[0] = 1;
            //    if (i == 1)
            //    {
            //        Stopwatch sw = Stopwatch.StartNew();
            //        sw.Start();
            //        while (true)
            //        {
            //            pmc_data.cdata[0] = 192;
            //            var ret = (Focas1.focas_ret)Focas1.pmc_wrpmcrng(cncHandle, 9, pmc_data);
            //            if (ret == Focas1.focas_ret.EW_OK)
            //            {
            //                isOk = true;
            //            }
            //            if (sw.ElapsedMilliseconds >= 500)
            //            {
            //                sw.Stop();
            //                break;
            //            }
            //        }
            //        //Task.Delay(500).ConfigureAwait(false);
            //    }
            //    else
            //    {
            //        pmc_data.cdata[0] = 64;
            //        var ret = (Focas1.focas_ret)Focas1.pmc_wrpmcrng(cncHandle, 9, pmc_data);
            //        if (ret == Focas1.focas_ret.EW_OK)
            //        {
            //            isOk = true;
            //        }
            //    }


            //}
            //if (isOk)
            //{

            //    MessageBox.Show("sucess");

            //}


        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            //ReadCnc()
            try
            {
                if (string.IsNullOrEmpty(txt_readAdr.Text))
                {
                    MessageBox.Show("输入的地址不能为空");
                }
                else
                {
                    // ushort a;ushort b;
                    var a = ushort.Parse(txt_readAdr.Text);
                    var result = ReadCnc(a, a, 5);
                    if (result != null)
                    {
                        MessageBox.Show($"rsult:{result.cdata[0]}");
                        //if (result.cdata[0] == 32 || result.cdata[0] == 64)
                        //{                          
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_doorActByMcode_Click(object sender, EventArgs e)
        {
            //pmc_get_current_pmc_unit 获取当前的 PMC 单元类型
            //pmc_select_pmc_unit 选择 PMC

            //It is possible to use in MDI mode.

            //如果模式不为MDI 先切换模式                   

            #region cnc_wropnlsgnl
            Focas1.IODBSGNL sgnl = new Focas1.IODBSGNL();
            sgnl.type = (short)Convert.ToInt32("0000000000000001", 2);
            //sgnl.mode = 0;// MDI
            sgnl.mode = 1;// MDI
            ret = Focas1.cnc_wropnlsgnl(cncHandle, sgnl);
            if (ret == 0)
            {
                MessageBox.Show("mode=1");
                //写 M 代码组数据
                #region cnc_wrmgrpdata
                #endregion
            }
            #endregion

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public int almdsta;//报警标识号
        private string paramFromFile = "";
        public Form1()
        {
            InitializeComponent();
        }

        private void connection_Click(object sender, EventArgs e)
        {
            this.ret = Focas1.cnc_allclibhndl3(ip.Text.ToString(), Convert.ToUInt16(port.Text.ToString()), Convert.ToInt32(timeout), out cncHandle);
            if (this.ret != Focas1.EW_OK)
            {
                //设备未连接上
                MessageBox.Show("设备未连接上");
            }
            else if (this.ret == Focas1.EW_OK)
            {
                MessageBox.Show("设备连接成功");
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            #region 坐标

            Focas1.ODBPOS fos = new Focas1.ODBPOS();
            short num = Focas1.MAX_AXIS;
            short type = -1;
            short ret = Focas1.cnc_rdposition(cncHandle, type, ref num, fos);
            if (ret == 0)
            {
                //绝对
                txtXAbsolutely.Text = (fos.p1.abs.data * Math.Pow(10, -fos.p1.abs.dec)).ToString();
                txtYAbsolutely.Text = (fos.p2.abs.data * Math.Pow(10, -fos.p2.abs.dec)).ToString();
                txtZAbsolutely.Text = (fos.p3.abs.data * Math.Pow(10, -fos.p3.abs.dec)).ToString();
                //相对
                txtXRelative.Text = (fos.p1.rel.data * Math.Pow(10, -fos.p1.rel.dec)).ToString();
                txtYRelative.Text = (fos.p2.rel.data * Math.Pow(10, -fos.p2.rel.dec)).ToString();
                txtZRelative.Text = (fos.p3.rel.data * Math.Pow(10, -fos.p3.rel.dec)).ToString();
                //机械
                txtXMach.Text = (fos.p1.mach.data * Math.Pow(10, -fos.p1.mach.dec)).ToString();
                txtYMach.Text = (fos.p2.mach.data * Math.Pow(10, -fos.p2.mach.dec)).ToString();
                txtZMach.Text = (fos.p3.mach.data * Math.Pow(10, -fos.p3.mach.dec)).ToString();
            }

            #endregion
            //系统信息
            Focas1.ODBSYS k1 = new Focas1.ODBSYS();
            ret = Focas1.cnc_sysinfo(cncHandle, k1);
            if (ret == Focas1.EW_OK)
            {
                MaxAxis = k1.max_axis.ToString();//最大轴数 
                this.txtMaxAxis.Text = MaxAxis;
                string type1 = k1.cnc_type[0].ToString() + k1.cnc_type[1].ToString();//CNC类型
                #region 机床系统类型判断
                switch (type1)
                {
                    case "15":
                        CNCType = "Series 15/15i";
                        break;
                    case "16":
                        CNCType = "Series 16/16i";
                        break;
                    case "18":
                        CNCType = "Series 18/18i";
                        break;
                    case "21":
                        CNCType = "Series 21/21i";
                        break;
                    case "30":
                        CNCType = "Series 30i";
                        break;
                    case "31":
                        CNCType = "Series 31i";
                        break;
                    case "32":
                        CNCType = "Series 32i";
                        break;
                    case "35":
                        CNCType = "Series 35i";
                        break;
                    case " 0":
                        CNCType = "Series 0i";
                        break;
                    case "PD":
                        CNCType = "Power Mate i-D";
                        break;
                    case "PH":
                        CNCType = "Power Mate i-H";
                        break;
                    case "PM":
                        CNCType = "Power Motion i";
                        break;
                    default:
                        CNCType = "其它类型";
                        break;
                }
                #endregion
                this.txtCNCType.Text = CNCType;
                MTType = k1.mt_type[0].ToString() + k1.mt_type[1].ToString();
                SerialNumber = k1.series[0].ToString() + k1.series[1].ToString() + k1.series[2].ToString() + k1.series[3].ToString();//CNC型号
                this.txtSerialNumber.Text = SerialNumber;
                Version = k1.version[0].ToString() + k1.version[1].ToString() + k1.version[2].ToString() + k1.version[3].ToString();
                Axis = k1.axes[0].ToString() + k1.axes[1].ToString();
            }
            Focas1.ODBST statinfo = new Focas1.ODBST();
            ret = Focas1.cnc_statinfo(cncHandle, statinfo);
            if (ret == Focas1.EW_OK)
            {
                //设备状态的判定方法：如果Alarm不为0则有报警。当没有报警时，如果run=3认为是在运行，其他都为待机
                run = statinfo.run;
                Alarm = statinfo.alarm;
                //MTMode = statinfo.tmmode;
                if (Alarm != 0)
                    run = 5;//5为设备报警状态
                this.txtDevStatus.Text = run.ToString();
                #region 工作模式判断
                switch (statinfo.aut)
                {
                    case 0:
                        CNCModel = "MDI";
                        break;
                    case 1:
                        CNCModel = "MEMory";
                        break;
                    case 2:
                        CNCModel = "Not Defined";
                        break;
                    case 3:
                        CNCModel = "EDIT";
                        break;
                    case 4:
                        CNCModel = "HaNDle";
                        break;
                    case 5:
                        CNCModel = "JOG";
                        break;
                    case 6:
                        CNCModel = "Teach in JOG";
                        break;
                    case 7:
                        CNCModel = "Teach in HaNDle";
                        break;
                    case 8:
                        CNCModel = "INC·feed";
                        break;
                    case 9:
                        CNCModel = "REFerence";
                        break;
                    case 10:
                        CNCModel = "ReMoTe";
                        break;
                    default:
                        CNCModel = "others mode";
                        break;
                }
                #endregion
                this.txtCNCModel.Text = CNCModel.ToString();
            }
            #region 报警数据
            ret = Focas1.cnc_alarm2(cncHandle, out almdsta);//Focas1.cnc_alarm2(h, out almdsta);
            if (ret == Focas1.EW_OK)
            {
                #region 报警判断
                switch (almdsta)
                {
                    case 0:
                        AlarmMessage = "参数开启（SW）";
                        break;
                    case 1:
                        AlarmMessage = "关机参数设置（PW）";
                        break;
                    case 2:
                        AlarmMessage = "I / O错误（IO）";
                        break;
                    case 3:
                        AlarmMessage = "前景P / S（PS";
                        break;
                    case 4:
                        AlarmMessage = "超程，外部数据（OT";
                        break;
                    case 5:
                        AlarmMessage = "过热报警（OH）";
                        break;
                    case 6:
                        AlarmMessage = "伺服报警（SV";
                        break;
                    case 7:
                        AlarmMessage = "数据I / O错误（SR）";
                        break;
                    case 8:
                        AlarmMessage = "宏指令报警（MC";
                        break;
                    case 9:
                        AlarmMessage = "主轴报警（SP）";
                        break;
                    case 10:
                        AlarmMessage = "其他警报（DS）";
                        break;
                    case 11:
                        AlarmMessage = "有关故障防止功能（IE）的警报";
                        break;
                    case 12:
                        AlarmMessage = "背景P / S（BG）";
                        break;
                    case 13:
                        AlarmMessage = "同步错误（SN）";
                        break;
                    case 14:
                        AlarmMessage = "保留";
                        break;
                    case 15:
                        AlarmMessage = "外部报警信息（EX）";
                        break;
                    case 16:
                        AlarmMessage = "正向超程（软限位1）";
                        break;
                    default:
                        AlarmMessage = "未知错误";
                        break;
                }
                #endregion
            }
            #endregion

            Focas1.ODBPRO dbpro = new Focas1.ODBPRO();
            if (Focas1.EW_OK == Focas1.cnc_rdprgnum(cncHandle, dbpro))
            {
                Mainpg = dbpro.mdata;//主程序号
                this.txtMainpg.Text = Mainpg.ToString();
                Currentpg = dbpro.data;//当前运行程序号（子程序号）
                this.txtCurrentpg.Text = Currentpg.ToString();

            }
            //主轴实际转速S
            Focas1.ODBACT data = new Focas1.ODBACT();
            ret = Focas1.cnc_acts(cncHandle, data);
            if (ret == Focas1.EW_OK)
            {
                Speed = data.data.ToString();
                this.txtSpeed.Text = Speed.ToString();//单位r/min
            }

            //进给率F 切削实际速度
            Focas1.ODBACT ff = new Focas1.ODBACT();
            ret = Focas1.cnc_actf(cncHandle, ff);
            if (ret == Focas1.EW_OK)
            {
                FeedRate = ff.data; //单位 mm/min
                this.txtFeedRate.Text = FeedRate.ToString();
            }
            #region  取刀具号  oi系统不支持 获取失败
            //short number = 1;
            //Focas1.IODBTLDT c = new Focas1.IODBTLDT();
            //ret = Focas1.cnc_rdtooldata(h, 1, ref number, c);
            //if (ret == Focas1.EW_OK)
            //{
            //    if (c.data1 != null)
            //    {
            //        Tool_no = c.data1.tool_no;
            //    }
            //}
            #endregion

            //进给倍率
            Focas1.IODBPMC0 ig = new Focas1.IODBPMC0();
            ret = Focas1.pmc_rdpmcrng(cncHandle, 0, 1, 12, 13, 8 + 1 * 2, ig);
            if (ret == Focas1.EW_OK)
            {
                FeedOverRide = (100 - (ig.cdata[0] - 155)).ToString();//进给倍率   转换成百分比 为什么是155没搞懂，设备不同也可能不是155
                this.txtFeedOverRide.Text = FeedOverRide.ToString();
            }
            //单次工件计数 这个和控制板上的数量相对应      
            Focas1.ODBM bb = new Focas1.ODBM();
            ret = Focas1.cnc_rdmacro(cncHandle, 0xf3d, 0x0a, bb);
            if (ret == Focas1.EW_OK)


            {
                string PartCnt = (bb.mcr_val / 100000).ToString();
                this.txtPartCnt.Text = PartCnt.ToString();
            }
            #region 工件总数
            Focas1.IODBPSD_1 param6712 = new Focas1.IODBPSD_1();
            ret = Focas1.cnc_rdparam(cncHandle, 6712, 0, 8, param6712);
            if (ret == Focas1.EW_OK)
            {
                int totalparts = param6712.ldata;
                this.txttotalparts.Text = totalparts.ToString();
            }
            #endregion
            #region 获取切削时间
            Focas1.IODBPSD_1 param6753 = new Focas1.IODBPSD_1();
            Focas1.IODBPSD_1 param6754 = new Focas1.IODBPSD_1();
            ret = Focas1.cnc_rdparam(cncHandle, 6753, 0, 8 + 32, param6753);
            if (ret == Focas1.EW_OK)
            {
                int cuttingTimeSec = param6753.ldata / 1000;
                ret = Focas1.cnc_rdparam(cncHandle, 6754, 0, 8 + 32, param6754);
                if (ret == Focas1.EW_OK)
                {
                    int cuttingTimeMin = param6754.ldata;
                    int CutTime = cuttingTimeMin * 60 + cuttingTimeSec;
                    this.txtCutTime.Text = CutTime.ToString();
                }
            }
            #endregion
            #region 获取运行时间
            Focas1.IODBPSD_1 param6751 = new Focas1.IODBPSD_1();
            Focas1.IODBPSD_1 param6752 = new Focas1.IODBPSD_1();
            ret = Focas1.cnc_rdparam(cncHandle, 6751, 0, 8, param6751);
            if (ret == Focas1.EW_OK)
            {
                int workingTimeSec = param6751.ldata / 1000;
                ret = Focas1.cnc_rdparam(cncHandle, 6752, 0, 8, param6752);
                if (ret == Focas1.EW_OK)
                {
                    int workingTimeMin = param6752.ldata;
                    int CycSec = workingTimeMin * 60 + workingTimeSec;
                    this.txtworkingTime.Text = CycSec.ToString();
                }
            }
            #endregion
            #region 获取开机时间
            Focas1.IODBPSD_1 param6750 = new Focas1.IODBPSD_1();
            ret = Focas1.cnc_rdparam(cncHandle, 6750, 0, 8 + 32, param6750);
            if (ret == Focas1.EW_OK)
            {
                int PoweOnTime = param6750.ldata * 60;
                this.txtPoweOnTime.Text = PoweOnTime.ToString();
            }
            #endregion
            //读取完成后，释放连接
            Focas1.cnc_freelibhndl(cncHandle);
            //Focas1.cnc_exitprocess();//for linux
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s_number"></param>
        /// <param name="e_number"></param>
        /// <param name="idCode">1=F,2=Y,3=X,4=A(Message),5=R...</param>
        /// <returns></returns>
        private Focas1.IODBPMC0 ReadCnc(ushort s_number, ushort e_number, short idCode = 5)
        {
            Focas1.IODBPMC0 pmc_data = new Focas1.IODBPMC0();
            var ret = (Focas1.focas_ret)Focas1.pmc_rdpmcrng(cncHandle, idCode, 0, s_number, e_number, 9, pmc_data);
            if (ret == Focas1.focas_ret.EW_OK)
            {
                return pmc_data;
            }
            else
            {
                return null;
            }
        }
    }
}
