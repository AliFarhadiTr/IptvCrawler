using DB;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IptvCrawler
{
    public partial class Form1 : Form
    {
        private CookieContainer cookies;
        RestClient client;
        private const string version = "V2.0";
        public static string errorlog;
        private bool busy = false;
        private int log_limit = 100;
        private string out_file = "";
        private string countries_file = "countries.csv";
        private int error = 0, success = 0;
        private Dictionary<string, int> chanels;
        private int total = 0;
        private DATABASE db;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LogInit();
            About();
            OutputInit();
            db = new DATABASE(AppDomain.CurrentDomain.BaseDirectory + "data.db");

            cookies = new CookieContainer();
            client = new RestClient("https://iptvcat.com/");
            client.CookieContainer = cookies;

            client.FollowRedirects = true;

            checkedListBox1.DisplayMember = "Text";

            LoadSettigs();

            textBox1.Enabled = checkBox1.Checked;

        }

        private void LoadSettigs()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();
            var data = db.Table("configs").FetchAll();

            foreach (NameValueCollection setting in data)
            {
                settings[setting["name"]] = setting["value"];
            }

            richTextBox1.Text = settings["find"];
            richTextBox2.Text = settings["replace"];
            richTextBox3.Text = settings["append_first"];
            richTextBox4.Text = settings["append_all"];

            numericUpDown2.Value = int.Parse(settings["min_quality"]);
            numericUpDown3.Value = int.Parse(settings["max_quality"]);
            comboBox1.SelectedIndex = int.Parse(settings["status"]);
            numericUpDown1.Value = int.Parse(settings["delay"]);
            numericUpDown4.Value = int.Parse(settings["max_chanel"]);
            add_count_checkBox.Checked = bool.Parse(settings["add_count"]);

            checkBox1.Checked = bool.Parse(settings["git"]);
            textBox1.Text = settings["git_token"];

            string[] countries = settings["countries"].Split(',');
            LoadCountries(countries);

        }

        private void SaveSettigs()
        {

            db.Table("configs").Update(
                new Dictionary<string, string> {
                    { "value",checkBox1.Checked.ToString()},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
                },
                new Dictionary<string, string>
                {
                    { "name","git"}
                });
            db.Table("configs").Update(
                new Dictionary<string, string> {
                    { "value",textBox1.Text},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
                },
                new Dictionary<string, string>
                {
                    { "name","git_token"}
                });


            db.Table("configs").Update(
                new Dictionary<string, string> {
                    { "value",richTextBox1.Text},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
                },
                new Dictionary<string, string>
                {
                    { "name","find"}
                });

            db.Table("configs").Update(
               new Dictionary<string, string> {
                    { "value",richTextBox2.Text},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
               },
               new Dictionary<string, string>
               {
                    { "name","replace"}
               });

            db.Table("configs").Update(
               new Dictionary<string, string> {
                    { "value",numericUpDown2.Value.ToString()},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
               },
               new Dictionary<string, string>
               {
                    { "name","min_quality"}
               }
            );

            db.Table("configs").Update(
               new Dictionary<string, string> {
                    { "value",numericUpDown3.Value.ToString()},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
               },
               new Dictionary<string, string>
               {
                    { "name","max_quality"}
               }
            );

            db.Table("configs").Update(
               new Dictionary<string, string> {
                    { "value",comboBox1.SelectedIndex.ToString()},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
               },
               new Dictionary<string, string>
               {
                    { "name","status"}
               }
            );

            db.Table("configs").Update(
               new Dictionary<string, string> {
                    { "value",numericUpDown1.Value.ToString()},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
               },
               new Dictionary<string, string>
               {
                    { "name","delay"}
               }
            );

            db.Table("configs").Update(
               new Dictionary<string, string> {
                    { "value",numericUpDown4.Value.ToString()},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
               },
               new Dictionary<string, string>
               {
                    { "name","max_chanel"}
               }
            );

            StringBuilder countries = new StringBuilder();
            foreach (CheckBox item in checkedListBox1.CheckedItems)
            {
                countries.Append(item.Tag).Append(",");
            }

            db.Table("configs").Update(
               new Dictionary<string, string> {
                    { "value",countries.ToString()},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
               },
               new Dictionary<string, string>
               {
                    { "name","countries"}
               }
            );

            db.Table("configs").Update(
               new Dictionary<string, string> {
                    { "value",richTextBox3.Text},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
               },
               new Dictionary<string, string>
               {
                    { "name","append_first"}
               }
            );

            db.Table("configs").Update(
               new Dictionary<string, string> {
                    { "value",richTextBox4.Text},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
               },
               new Dictionary<string, string>
               {
                    { "name","append_all"}
               }
            );

            db.Table("configs").Update(
               new Dictionary<string, string> {
                    { "value",add_count_checkBox.Checked.ToString()},
                    { "updated_at",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
               },
               new Dictionary<string, string>
               {
                    { "name","add_count"}
               }
            );

        }

        private void LoadCountries(string[] selected = null)
        {
            if (countries_file == "countries.csv") countries_file = AppDomain.CurrentDomain.BaseDirectory + countries_file;
            var lines = File.ReadLines(countries_file);

            checkedListBox1.Items.Clear();

            foreach (string line in lines)
            {
                string[] cl = line.Split(',');
                if (cl.Length > 1 && cl[0] != "" && cl[1] != "")
                {
                    CheckBox ch = new CheckBox();
                    ch.Text = cl[1];
                    ch.Tag = cl[0];
                    ch.Checked = false;
                    ch.CheckState = CheckState.Unchecked;

                    bool not_add = true;
                    if (selected != null)
                    {
                        foreach (var item in selected)
                        {
                            if (item == cl[0])
                            {
                                checkedListBox1.Items.Add(ch, true);
                                not_add = false;
                                break;
                            }
                        }
                    }

                    if (not_add)
                    {
                        checkedListBox1.Items.Add(ch, false);
                    }


                }
            }

            checkedListBox1_SelectedValueChanged(null, null);
        }

        private void OutputInit()
        {
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "output\\")) Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "output\\");

            out_file = AppDomain.CurrentDomain.BaseDirectory + "output\\0 channels.m3u";
            if (File.Exists(out_file))
            {
                File.Delete(out_file);
            }

        }

        private void LogInit()
        {
            string log_dir = AppDomain.CurrentDomain.BaseDirectory + "logs\\";

            if (!Directory.Exists(log_dir)) Directory.CreateDirectory(log_dir);

            DirectoryInfo info = new DirectoryInfo(log_dir);
            FileInfo[] files = info.GetFiles().OrderBy(p => p.CreationTime).ToArray();
            int ln = files.Length;
            int i = 0;
            foreach (FileInfo file in files)
            {
                if (ln < log_limit)
                {
                    break;
                }
                else
                {
                    if (i++ < (ln - log_limit)) file.Delete();
                }
            }

            errorlog = log_dir + DateTime.Now.ToString("yyyy_MM_dd__HH_mm_ss") + ".txt";

        }

        public static void Logger(Exception ex, string mymsg)
        {

            // Get stack trace for the exception with source file information
            var st = new StackTrace(ex, true);
            // Get the top stack frame
            var frame = st.GetFrame(0);
            // Get the line number from the stack frame
            string line = frame.GetFileLineNumber().ToString();
            string msg = ex.Message + " =>Line: " + line + " =>Cause: " + mymsg;
            File.AppendAllText(Form1.errorlog, string.Join(Environment.NewLine, new string[] { "*************" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "*************", msg, "------------------------------------", "" }));
            Console.WriteLine(msg);
        }

        private void About()
        {

            GroupBox grp = new GroupBox();

            PictureBox pic1 = new PictureBox();
            PictureBox pic2 = new PictureBox();
            Label lbl1 = new Label();
            Label lbl2 = new Label();
            Label lbl3 = new Label();

            // label1
            // 
            lbl1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            lbl1.AutoSize = true;
            lbl1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            lbl1.ForeColor = System.Drawing.Color.Coral;
            lbl1.Location = new System.Drawing.Point(this.Size.Width - 130, 20);
            lbl1.Name = "lbl1_name";
            lbl1.Size = new System.Drawing.Size(75, 13);
            lbl1.TabIndex = 0;
            lbl1.Text = "برنامه نویس:";

            // 
            // label2
            // 
            lbl3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            lbl3.AutoSize = true;
            lbl3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            lbl3.ForeColor = System.Drawing.Color.CornflowerBlue;
            lbl3.Location = new System.Drawing.Point(this.Size.Width - 210, 20);
            lbl3.Name = "lbl3_name";
            lbl3.Size = new System.Drawing.Size(77, 13);
            lbl3.TabIndex = 0;
            lbl3.Text = "علی فرهادی";

            // label3
            // 
            lbl2.AutoSize = true;
            lbl2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            lbl2.ForeColor = System.Drawing.Color.Crimson;
            lbl2.Location = new System.Drawing.Point(6, 20);
            lbl2.Name = "lbl2_name";
            lbl2.Size = new System.Drawing.Size(33, 13);
            lbl2.TabIndex = 0;
            lbl2.Text = version;
            // 

            // 
            // pictureBox2
            // 
            pic1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            pic1.Cursor = System.Windows.Forms.Cursors.Hand;
            pic1.Image = Properties.Resources.insta;
            pic1.Location = new System.Drawing.Point(this.Size.Width - 250, 12);
            pic1.Name = "pic1_name";
            pic1.Size = new System.Drawing.Size(30, 30);
            pic1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pic1.TabIndex = 2;
            pic1.TabStop = false;
            pic1.Click += new System.EventHandler(this.pictureBox2_Click);
            // 
            // pictureBox1
            // 
            pic2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            pic2.Cursor = System.Windows.Forms.Cursors.Hand;
            pic2.Image = Properties.Resources.tlg;
            pic2.Location = new System.Drawing.Point(this.Size.Width - 290, 12);
            pic2.Name = "pic2_name";
            pic2.Size = new System.Drawing.Size(30, 30);
            pic2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pic2.TabIndex = 1;
            pic2.TabStop = false;
            pic2.Click += new System.EventHandler(this.pictureBox1_Click);

            // 
            // groupBox1
            // 
            grp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            grp.RightToLeft = RightToLeft;

            grp.Location = new System.Drawing.Point(12, this.Size.Height - 100);
            grp.Name = "grp_name";
            grp.Size = new System.Drawing.Size(this.Size.Width - 40, 50);
            grp.TabIndex = 0;
            grp.TabStop = false;
            grp.Text = "درباره ما";

            grp.Controls.Add(lbl1);
            grp.Controls.Add(lbl2);
            grp.Controls.Add(lbl3);

            grp.Controls.Add(pic1);
            grp.Controls.Add(pic2);

            this.Controls.Add(grp);

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.instagram.com/alifarhaditr/");
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Process.Start("https://t.me/ba_elec");
        }

        private void EnableInput(bool enable)
        {
            busy = !enable;
            button1.Enabled = enable;
            button2.Enabled = !enable;
            button3.Enabled = enable;
            button4.Enabled = enable;

            numericUpDown1.Enabled = enable;
            numericUpDown2.Enabled = enable;
            numericUpDown3.Enabled = enable;
            numericUpDown4.Enabled = enable;

            richTextBox1.Enabled = enable;
            richTextBox2.Enabled = enable;
            richTextBox3.Enabled = enable;
            richTextBox4.Enabled = enable;

            comboBox1.Enabled = enable;

            checkedListBox1.Enabled = enable;

            add_count_checkBox.Enabled = enable;

            checkBox1.Enabled = enable;
            textBox1.Enabled = enable;
        }

        //start
        private void button1_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> selected_countries = new Dictionary<string, string>();

            foreach (CheckBox ch in checkedListBox1.CheckedItems)
            {
                selected_countries.Add(ch.Text, ch.Tag.ToString());
            }

            if (selected_countries.Count == 0)
            {
                MessageBox.Show("هیچ کشوری انتخاب نشده است.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                return;

            }

            string _find = richTextBox1.Text.Replace("\n", Environment.NewLine);
            string _replace = richTextBox2.Text.Replace("\n", Environment.NewLine);

            String[] find = _find.Split(new[] { Environment.NewLine }
                                          , StringSplitOptions.RemoveEmptyEntries);
            String[] replace = _replace.Split(new[] { Environment.NewLine }
                                          , StringSplitOptions.RemoveEmptyEntries);

            if (find.Length != replace.Length)
            {
                MessageBox.Show("تعداد کلمات اصلی با کلمات جایگزین برابر نیست.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                return;

            }

            int delay = (int)numericUpDown1.Value;

            int min_quality = (int)numericUpDown2.Value;
            int max_quality = (int)numericUpDown3.Value;
            int max = (int)numericUpDown4.Value;

            int status = comboBox1.SelectedIndex;

            string append_first = richTextBox3.Text;
            string append_all = richTextBox4.Text;

            EnableInput(false);

            progressBar2.Value = 0;
            progressBar2.Maximum = selected_countries.Count;

            chanels = new Dictionary<string, int>();
            total = 0;
            OutputInit();

            /*
            Properties.Settings.Default.find = richTextBox1.Text;
            Properties.Settings.Default.replace = richTextBox2.Text;
            Properties.Settings.Default.Save();

            */

            new Task(() =>
            {

                try
                {
                    Main(selected_countries, max, min_quality, max_quality, status, delay, find, replace, append_first, append_all);

                }
                catch (Exception ex)
                {
                    Logger(ex, "unknown!");
                    MessageBox.Show("یک خطای غیر منتظره رخ داده است.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                }
                finally
                {
                    this.Invoke(new Action(() =>
                    {
                        EnableInput(true);
                        UpdateLables("...", 1);
                        progressBar1.Value = progressBar1.Maximum;
                        progressBar2.Value = progressBar2.Maximum;

                    }));
                }

            }).Start();
        }

        //stop
        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            busy = false;
        }

        private void Main(Dictionary<string, string> selected_countries, int max, int min_quality, int max_quality, int status, int delay, String[] find, String[] replace, string append_first, string append_all)
        {
            foreach (var country in selected_countries)
            {
                int page = 1;
                RestRequest request = new RestRequest();
                request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:76.0) Gecko/20100101 Firefox/76.0");

                RestRequest request2 = new RestRequest("ajax/streams_a?action=list");
                request2.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:76.0) Gecko/20100101 Firefox/76.0");
                request2.AddHeader("Referer", client.BaseUrl + country.Value + "/" + page.ToString());
                request2.AddParameter("to_del", "false");
                request2.AddParameter("sort", "false");

                cookies = new CookieContainer();
                client.CookieContainer = cookies;

                this.Invoke(new Action(() =>
                {
                    UpdateLables(country.Key, page);
                    progressBar2.Value++;

                }));

                int cnt = 0;
                while (true)
                {

                    request.Resource = country.Value + "/" + (page++).ToString();

                    try
                    {

                        if (!busy)
                        {
                            Saver(request, country.Key, find, replace, append_first, append_all);
                            MessageBox.Show("عملیات توسط کاربر لغو شد.", "پایان", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                            return;
                        }

                        this.Invoke(new Action(() =>
                        {
                            UpdateLables(country.Key, page);

                        }));


                        var response = client.Get(request);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {

                            if (response.Content.Contains("Nothing found!"))
                            {
                                break;
                            }

                            RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Singleline;
                            string pattern_ids = @"<span[^><]+?data-stream=""([0-9]+?)"">Add to list</span>";
                            string pattern_lives = @"<div class=""live green""[^><]+?>([0-9]+?)</div>";
                            string pattern_statuses = @"class=""state ([^""]+?)""";

                            MatchCollection ids = Regex.Matches(response.Content, pattern_ids, options);
                            MatchCollection lives = Regex.Matches(response.Content, pattern_lives, options);
                            MatchCollection statuses = Regex.Matches(response.Content, pattern_statuses, options);

                            this.Invoke(new Action(() =>
                            {
                                progressBar1.Value = 0;
                                progressBar1.Maximum = ids.Count;

                            }));

                            for (int i = 0; i < ids.Count; i++)
                            {
                                try
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        UpdateLables(country.Key, page);
                                        progressBar1.Value++;

                                    }));

                                    if (!busy)
                                    {
                                        Saver(request, country.Key, find, replace, append_first, append_all);
                                        MessageBox.Show("عملیات توسط کاربر لغو شد.", "پایان", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                                        return;
                                    }

                                    if (status == 0 && statuses[i].Groups[1].ToString() != "online") continue;

                                    if (status == 1 && statuses[i].Groups[1].ToString() != "Offline") continue;

                                    int live = Int16.Parse(lives[i].Groups[1].ToString());

                                    if (live < min_quality || live > max_quality) continue;

                                    if (chanels.ContainsKey(ids[i].Groups[1].ToString())) continue;

                                    chanels.Add(ids[i].Groups[1].ToString(), live);

                                    request2.AddOrUpdateParameter("items[]", ids[i].Groups[1].ToString());
                                    response = client.Post(request2);

                                    success++;
                                    cnt++;

                                    if (cnt >= max) break;
                                }
                                catch (Exception ex)
                                {
                                    Logger(ex, "country:'" + country + "' for(int i=0;i<ids.Count;i++)");
                                    error++;
                                }
                            }

                        }
                        else
                        {
                            throw new Exception("main:response wrong status=>" + response.StatusCode.ToString());
                        }

                    }
                    catch (Exception ex)
                    {
                        Logger(ex, "country:'" + country + "' foreach (var country in selected_countries)");
                        error++;
                    }

                    Delay(delay);
                    if (cnt >= max) break;
                }

                Saver(request, country.Key, find, replace, append_first, append_all);
            }

            this.Invoke(new Action(() =>
            {
                if (checkBox1.Checked)
                {
                    try
                    {
                        Upload2Git(textBox1.Text);
                        MessageBox.Show("فایل با موفقیت بر روی گیت هاب قرار گرفت.", "موفق", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                    }
                    catch (Exception ex)
                    {

                        Logger(ex,"github upload error!");
                        MessageBox.Show("خطای در آپلود فایل بر روی گیت هاب رخ داده است.", "خطا در گیت هاب", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                    }

                }
                
            }));

            MessageBox.Show("کلیه موارد با موفقیت دریافت شد.", "موفق", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

        }

        private void Upload2Git(string token)
        {

            var basicAuth = new Octokit.Credentials(token);
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("iptvscraper_by_alifarhadi"));
            client.Credentials = basicAuth;

            var new_reps = new Octokit.NewRepository(DateTime.Now.ToString("yyyy"));
            new_reps.Visibility = Octokit.RepositoryVisibility.Public;

            var all_repos = client.Repository.GetAllForCurrent();
            all_repos.Wait();

            bool rep_found = false;
            long rep_id = 0;
            foreach (var rep in all_repos.Result)
            {
                if (rep.Name == new_reps.Name)
                {
                    rep_found = true;
                    rep_id = rep.Id;
                    break;
                }
            }

            string month = DateTime.Now.ToString("MMM");
            string file_name = Path.GetFileName(out_file);
            if (!rep_found)
            {
                var rep_t = client.Repository.Create(new_reps);
                rep_t.Wait();
                rep_id = rep_t.Result.Id;
            }

            var waiter = client.Repository.Content.GetAllContents(rep_id, month);
            waiter.Wait();

            string txt = File.ReadAllText(out_file);

            foreach (var item in waiter.Result)
            {
                if(item.Name== file_name)
                {
                    var up_file1 = new Octokit.UpdateFileRequest("uploaded by script:" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), txt,item.Sha);
                    var file_res1 = client.Repository.Content.UpdateFile(rep_id, month + "/" + file_name, up_file1);
                    file_res1.Wait();
                    System.Diagnostics.Process.Start(file_res1.Result.Content.DownloadUrl);
                    return;
                }
            }
            
            var up_file = new Octokit.CreateFileRequest("uploaded by script:" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), txt);
            var file_res=client.Repository.Content.CreateFile(rep_id,  month+ "/" + file_name, up_file);
            file_res.Wait();
            string raw=file_res.Result.Content.DownloadUrl;
            System.Diagnostics.Process.Start(raw);

        }

        private void Saver(RestRequest request, string country, String[] find, String[] replace, string append_first, string append_all)
        {

            try
            {

                var response = client.Get(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Singleline;
                    string pattern = @"href=""[^""]*?my_list/([^""]+?)""[^<>]+?title=""Download list""";

                    MatchCollection links = Regex.Matches(response.Content, pattern, options);

                    request.Resource = "my_list/" + links[0].Groups[1].ToString();

                    byte[] data = client.DownloadData(request);

                    string content = System.Text.Encoding.UTF8.GetString(data);

                    string pattern_title = @"group-title=""([^""]*?)""";

                    MatchCollection titles = Regex.Matches(content, pattern_title, options);

                    total += titles.Count;

                    if (File.Exists(out_file))
                    {
                        content = content.Replace("#EXTM3U\n", "");
                        content = content.Replace("#PLAYLIST:iptvcat.com\n", "");
                        content = append_all + "\n" + content;
                        if (add_count_checkBox.Checked) content = Regex.Replace(content, @"group-title=""[^""]*?""", String.Format(@"group-title=""{0}({1})""", country, titles.Count));
                        else content = Regex.Replace(content, @"group-title=""[^""]*?""", String.Format(@"group-title=""{0}""", country));

                    }
                    else
                    {

                        if (content.Contains("#PLAYLIST:iptvcat.com\n"))
                        {
                            content = content.Replace("#PLAYLIST:iptvcat.com\n", "#PLAYLIST:iptvcat.com\n" +
                            append_all + "\n");

                            if (add_count_checkBox.Checked) content = Regex.Replace(content, @"group-title=""[^""]*?""", String.Format(@"group-title=""{0}({1})""", country, titles.Count));
                            else content = Regex.Replace(content, @"group-title=""[^""]*?""", String.Format(@"group-title=""{0}""", country));

                            content = content.Replace("#PLAYLIST:iptvcat.com\n", "#PLAYLIST:iptvcat.com\n" +
                            append_first + "\n");

                        }
                        else
                        {
                            content = content.Replace("#EXTM3U\n", "#PLAYLIST:iptvcat.com\n" +
                            append_all + "\n");

                            if (add_count_checkBox.Checked) content = Regex.Replace(content, @"group-title=""[^""]*?""", String.Format(@"group-title=""{0}({1})""", country, titles.Count));
                            else content = Regex.Replace(content, @"group-title=""[^""]*?""", String.Format(@"group-title=""{0}""", country));

                            content = content.Replace("#PLAYLIST:iptvcat.com\n", "#PLAYLIST:iptvcat.com\n" +
                            append_first + "\n");
                        }

                    }

                    content = content.Replace("#__COUNTRY__#", country);

                    for (int i = 0; i < find.Length; i++)
                    {
                        content = content.Replace(find[i], replace[i]);
                    }

                    File.AppendAllText(out_file, content);

                    string new_name = Regex.Replace(out_file, @"[0-9]+? channels\.m3u", total.ToString() + " channels.m3u");
                    File.Delete(new_name); // Delete the existing file if exists
                    File.Move(out_file, new_name); // Rename the oldFileName into newFileName
                    out_file = new_name;

                }
                else
                {
                    throw new Exception("response wrong status=>" + response.StatusCode.ToString());
                }
            }
            catch (Exception ex)
            {
                Logger(ex, "country:'" + country + "' Saver");
                error++;
            }
        }

        private void Delay(int delay)
        {
            int d = 0;
            while (d++ < delay && busy)
            {
                Thread.Sleep(1000);
            }
        }

        private void UpdateLables(string current, int page)
        {
            label8.Text = success.ToString();
            label10.Text = error.ToString();

            label12.Text = current;
            label14.Text = (page - 1).ToString();
            label18.Text = total.ToString();
        }

        private void checkedListBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            label4.Text = "(" + checkedListBox1.CheckedItems.Count.ToString() + ")";
        }

        private void checkedListBox1_Click(object sender, EventArgs e)
        {
            label4.Text = "(" + checkedListBox1.CheckedItems.Count.ToString() + ")";
        }

        private void checkedListBox1_DoubleClick(object sender, EventArgs e)
        {
            label4.Text = "(" + checkedListBox1.CheckedItems.Count.ToString() + ")";
        }

        //reset categories
        private void button3_Click(object sender, EventArgs e)
        {
            LoadCountries();
            label4.Text = "(0)";
        }

        //save setting
        private void button4_Click(object sender, EventArgs e)
        {
            SaveSettigs();
            MessageBox.Show("تنظیمات با موفقیت ذخیره شد.", "موفق", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

        }

        private void label26_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Enabled = ((CheckBox)sender).Checked;
        }

        private void label25_Click(object sender, EventArgs e)
        {

        }

        private void checkedListBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            label4.Text = "(" + checkedListBox1.CheckedItems.Count.ToString() + ")";
        }
    }
}
