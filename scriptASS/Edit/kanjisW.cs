using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace scriptASS
{
    public partial class kanjisW : Form
    {
        mainW mW;
        ArrayList Ks = new ArrayList();
        readonly string MarcaMismo = "[mismo]";

        public kanjisW(mainW m)
        {
            InitializeComponent();
            mW = m;
            kanjisBox.KeyDown += new KeyEventHandler(kanjisBox_KeyDown);              
        }

        void kanjisBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.End:
                case Keys.Delete:
                case Keys.Insert: // insert+pgdn ^^                    

                    string s = (e.KeyCode == Keys.Delete) ? MarcaMismo : kanjisBox.SelectedText;
                    if (kanjiGrid.SelectedRows.Count > 0)
                    {
                        if (kanjiGrid.SelectedRows[0].Index == 0 && e.KeyCode == Keys.Delete)
                        {
                            mW.errorMsg("No se puede usar la funcionalidad '[mismo]' con la primera línea");
                            break;
                        }
                        if (e.KeyCode==Keys.End)
                            kanjiGrid["Kanjis", kanjiGrid.SelectedRows[0].Index].Value += s;
                        else 
                            kanjiGrid["Kanjis", kanjiGrid.SelectedRows[0].Index].Value = s;
                        
                        if (e.KeyCode == Keys.Insert || e.KeyCode == Keys.End)
                        {
                            kanjisBox.SelectionStart = kanjisBox.SelectionStart + kanjisBox.SelectionLength;
                            kanjisBox.SelectionLength = 1;
                        }
                        goto tirapabajo;
                        updateScroll();
                    }
                    break;
                case Keys.PageDown:
                    tirapabajo:
                    if (kanjiGrid.SelectedRows.Count > 0)
                    {
                        if (kanjiGrid.SelectedRows[0].Index < kanjiGrid.Rows.Count - 1)
                        {
                            kanjiGrid.Rows[kanjiGrid.SelectedRows[0].Index + 1].Selected = true;
                        }
                        updateScroll();
                        e.Handled = true;
                    }
                    break;
                case Keys.PageUp:
                    if (kanjiGrid.SelectedRows.Count > 0)
                    {
                        if (kanjiGrid.SelectedRows[0].Index > 0)
                        {
                            kanjiGrid.Rows[kanjiGrid.SelectedRows[0].Index - 1].Selected = true;
                        }
                        updateScroll();
                        e.Handled = true;
                    }
                    break;
                case Keys.Left:
                    if (kanjisBox.SelectionStart>0) kanjisBox.SelectionStart--; 
                    kanjisBox.SelectionLength = 1;
                    e.Handled = true;
                    break;
                case Keys.Right:
                    kanjisBox.SelectionStart++;
                    kanjisBox.SelectionLength = 1;
                    e.Handled = true;
                    break;
            }
        }

        private void updateScroll()
        {
            if (kanjiGrid.SelectedRows.Count<=0) return;
            if (kanjiGrid.SelectedRows[0].Index > 15)
                kanjiGrid.FirstDisplayedScrollingRowIndex = kanjiGrid.SelectedRows[0].Index - 14;

        }


        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Archivos soportados (*.txt;*.ass;*.ssa)|*.txt;*.ass;*.ssa";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                StreamReader sr = FileAccessWrapper.OpenTextFile(ofd.FileName);
                FileInfo finfo = new FileInfo(ofd.FileName);
                kanjisBox.Text = "";
                string ext = finfo.Extension.ToLower();
                switch (ext)
                {
                    case ".txt":
                        kanjisBox.Text = sr.ReadToEnd();
                        break;
                    case ".ssa":
                    case ".ass":
                        string linea;
                        while ((linea = sr.ReadLine()) != null)
                        {
                            if (linea.StartsWith("Dialogue", StringComparison.InvariantCultureIgnoreCase))
                            {
                                lineaASS lass = new lineaASS(linea);
                                kanjisBox.Text = kanjisBox.Text + lineaASS.cleanText(lass.texto);
                            }
                        }
                        break;
                    default:
                        mW.errorMsg("Extensión desconocida.");
                        break;
                }
               
                sr.Close();
            }
        }

        private void LineaASS_to_Karaoke_K()
        {
            for (int i = 0; i < mW.al.Count; i++)
            {
                lineaASS lass = (lineaASS)mW.al[i];
                string s = lass.texto;
                Regex r = new Regex(@"\{\\[kK]f?\d+\}");
                MatchCollection mc = r.Matches(s);

                for (int x = 0; x < mc.Count; x++)
                {
                    Match m = mc[x];
                    int fin_actual = m.Index + m.Length;
                    string texto = "";

                    // obtener texto :)
                    if (x < mc.Count - 1)
                    {
                        Match siguiente = mc[x + 1];
                        int principio_siguiente = siguiente.Index;
                        texto = s.Substring(fin_actual, principio_siguiente - fin_actual);
                    }
                    else
                        texto = s.Substring(fin_actual);


                    // estilo K
                    Regex styleh = new Regex("[a-zA-Z]+");
                    Match sss = styleh.Match(m.ToString());
                    string estilok = sss.ToString();

                    // nº milisegundos
                    Regex milis = new Regex(@"\d+");
                    Match mmm = milis.Match(m.ToString());
                    int milisec = int.Parse(mmm.ToString());

                    Ks.Add(new Karaoke_K(i, estilok, milisec, texto));
                }
            }
        }

        private void DisplayKinGrid()
        {
            kanjiGrid.RowCount = Ks.Count;

            for (int i = 0; i < Ks.Count; i++)
            {
                Karaoke_K k = (Karaoke_K)Ks[i];
                kanjiGrid["NumLinea", i].Value = k.LineIndex;
                kanjiGrid["K", i].Value = k.StyleK;
                kanjiGrid["Tiempo", i].Value = k.Milliseconds;
                kanjiGrid["Rom", i].Value = k.Text;
            }
        }

        private void UpdateKfromGrid()
        {
            for (int i = 0; i < Ks.Count; i++)
            {
                Karaoke_K k = (Karaoke_K)Ks[i];
                k.LineIndex = int.Parse(kanjiGrid["NumLinea", i].Value.ToString());
                k.StyleK = kanjiGrid["K", i].Value.ToString();
                k.Milliseconds = int.Parse(kanjiGrid["Tiempo", i].Value.ToString());
                k.Text = kanjiGrid["Rom", i].Value.ToString();
                try
                {
                    k.Kanjis = kanjiGrid["Kanjis", i].Value.ToString();
                }
                catch { }
                
                if (k.Kanjis.Equals(MarcaMismo) && i>0)
                {
                    // hay que obtener el primero no-nulo
                    int marca = 0, suma_acumulada = 0;
                    for (int x = i - 1; x >= 0; x--)
                    {
                        Karaoke_K tmp = (Karaoke_K)Ks[x];
                        if (!tmp.Kanjis.Equals(MarcaMismo)){
                            marca = x;
                            break;
                        }
                        suma_acumulada += tmp.Milliseconds;
                    }

                    Karaoke_K anterior = (Karaoke_K)Ks[marca];
                    anterior.Kanjis = anterior.Kanjis;
                    anterior.Milliseconds += k.Milliseconds;

                }

            }

        }

        private void kanjisW_Load(object sender, EventArgs e)
        {
            this.MaximumSize = this.Size;
            LineaASS_to_Karaoke_K();
            DisplayKinGrid();
        }


        private void button2_Click(object sender, EventArgs e)
        {

            string seleccionado = kanjisBox.Text.Substring(kanjisBox.SelectionStart, kanjisBox.SelectionLength);


            //char[] bleh = seleccionado.ToCharArray();
            // kanjisBox.Focus();
            
            // SendKeys.Send(seleccionado);
                

            /*
            kanjisBox.Focus();
            int hIMC = 0;
            IntPtr hwndptr = kanjisBox.Handle;
            int hwnd = hwndptr.ToInt32();
            hIMC = IMM32Wrapper.ImmGetContext(hwnd);
            bool b = IMM32Wrapper.ImmSetOpenStatus(hIMC, true);
            byte[] selected = Encoding.Unicode.GetBytes(seleccionado);
            int brau = IMM32Wrapper.ImmSetCompositionStringW(hIMC, IMM32Wrapper.SCS_SETSTR, selected, selected.Length, null, 0);
            //int brau = IMM32Wrapper.ImmSetCompositionStringW(hIMC, IMM32Wrapper.SCS_SETSTR, selected_unc, 0, null, 0);
            if (brau == 0) MessageBox.Show("Este hijo de puta no se deja hacer");
            int len = IMM32Wrapper.ImmGetCompositionStringW(hIMC, IMM32Wrapper.GCS_RESULTREADSTR, null, 0);
            if (len > 0)
            {
                byte[] bytearray = new byte[len * 2];
                IMM32Wrapper.ImmGetCompositionStringW(hIMC, IMM32Wrapper.GCS_RESULTREADSTR, bytearray, len);
                furiganaBox.Text = Encoding.Unicode.GetString(bytearray);
            }


            IMM32Wrapper.ImmReleaseContext(hwnd, hIMC);*/

        }

        private void button3_Click(object sender, EventArgs e)
        {                        
            if (sustituir.Checked) MessageBox.Show("Atención: Vas a sustituir el archivo SSA que contenía el roomaji timeado por el de los Kanjis.\nTen cuidado y acuérdate de guardarlo usando 'Guardar cómo...' y otro nombre de archivo.", "perrySub", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            mW.UndoRedo.AddUndo(mW.script, "Aplicación timer de kanjis");
            UpdateKfromGrid();
            string[] lines = new string[mW.al.Count];

            for (int i = 0; i < kanjiGrid.Rows.Count; i++)
            {
                Karaoke_K kkk = (Karaoke_K)Ks[i];

                int j = kkk.LineIndex;

                if (!kkk.Kanjis.Equals(MarcaMismo))
                {
                    lines[j] = (lines[j] != null)? lines[j] + kkk.ToString() : kkk.ToString();
                }
            }

            for (int i = 0; i < lines.Length; i++)
            {
                lineaASS lass = (lineaASS)mW.al[i];
                if (lines[i] != null)
                {
                    if (sustituir.Checked) lass.texto = lines[i].Replace(MarcaMismo, "");
                    else
                    {
                        lineaASS nueva = (lineaASS)lass.Clone();
                        nueva.texto = lines[i].Replace(MarcaMismo, "");
                        mW.al.Add(nueva);
                    }
                }
            }
            
            mW.Enabled = true;
            mW.updateGridWithArrayList(mW.al);
            mW.refreshGrid();
            this.Dispose();


        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("¿Seguro que deseas salir del asistente?", "perrySub", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.Dispose();
            }
        }

    }
}