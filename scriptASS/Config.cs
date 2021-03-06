using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;

namespace scriptASS
{
    public class ConfigException : PerrySubException
    {
        public ConfigException() : base() { }
        public ConfigException(string m) : base(m) { }
    }
    class Config
    {
        readonly char separador = ',';
        const int limite = 20;
        readonly string[] reservadas = { ".part", ".count", "comment" };
        int numcomentarios = 0;
        int ordenentrada = 0;

        Hashtable cfg;
        string filename;

        public Config(string fn)
        {
            cfg = new Hashtable();
            filename = fn;
            if (!File.Exists(fn))
            {
                TextWriter o = new StreamWriter(filename, false, System.Text.Encoding.UTF8);
                o.WriteLine("; Archivo de configuración generado por PerrySub. No modificar. \t(c) mrm/AnimeUnderground");
                o.Close();
            }

        }

        internal class ConfigEntry : IComparable
        {
            int position;
            string key;
            string entry;

            internal int Position
            {
                get { return position; }
            }

            internal string Key
            {
                get { return key; }
                set { key = value; }
            }

            internal string Entry
            {
                get { return entry; }
                set { entry = value; }
            }

            internal ConfigEntry(int Position, string Key, string Entry)
            {
                this.position = Position;
                this.key = Key;
                this.entry = Entry;
            }


            #region IComparable Members

            public int CompareTo(object obj)
            {
                if (!(obj is ConfigEntry))
                    throw new InvalidCastException();

                ConfigEntry elOtro = (ConfigEntry)obj;
                return elOtro.Position.CompareTo(this.position);                    
            }

            #endregion
        }

        public void LoadConfig()
        {
            StreamReader sr = FileAccessWrapper.OpenTextFile(filename);
            string linea = "";

            while ((linea = sr.ReadLine()) != null)
            {
                if (linea.StartsWith(";"))
                {
                    SetValue("comment.part" + (numcomentarios++), linea.Substring(1),true);
                }
                else
                {
                    string[] s = linea.Split(new char[] { '=' }, 2);
                    SetValue(s[0], s[1],true);
                }
            }
            sr.Close();
        }

        public void WriteConfig()
        {
            ArrayList tmp = new ArrayList(cfg.Count);
            foreach (DictionaryEntry d in cfg)
            {
                string key = d.Key.ToString();
                ConfigEntry entry = (ConfigEntry)d.Value;

                if (d.Key.ToString().StartsWith("comment"))
                    tmp.Add(";" + entry.Entry);
                else
                    tmp.Add(key + "=" + entry.Entry);
            }

            tmp.Sort();

            TextWriter o = new StreamWriter(filename, false, System.Text.Encoding.UTF8);
            foreach (string s in tmp)
                o.WriteLine(s);
            o.Close();
        }

        private bool ValidateKey(string key)
        {
            foreach (string s in reservadas)
                if (key.Contains(s)) return false;

            return true;
        }

        public bool ExistsKey(string key)
        {
            return cfg.ContainsKey(key);
        }

        public void DeleteKey(string key)
        {
            if (!ExistsKey(key)) throw new ConfigException("No se ha encontrado la entrada: " + key);
            cfg.Remove(key);
        }

        public string GetValue(string key)
        {
            if (!ExistsKey(key)) throw new ConfigException("Valor no encontrado: " + key);

            return ((ConfigEntry)cfg[key]).Entry;
        }

        public int GetNumValue(string key)
        {
            return int.Parse(GetValue(key));
        }

        public void SetValue(string key, string value)
        {
            if (!ValidateKey(key)) throw new ConfigException("La clave contiene palabras reservadas: " + key);
            SetValue(key, value, false);

        }
        private void SetValue(string key, string value, bool isinternal)
        {
            if (ExistsKey(key))
                DeleteKey(key);

            cfg.Add(key, new ConfigEntry(ordenentrada++, key, value));
        }

        public ArrayList GetValueA(string key)
        {
            if (ExistsKey(key + ".count") && (!key.Contains(".part")))
            {
                int nentradas = int.Parse(GetValue(key + ".count"));
                ArrayList tmp = new ArrayList();
                
                for (int i = 0; i < nentradas; i++)
                    tmp.AddRange(GetValueA(key +".part"+ i));
                return tmp;
            }
            else
            {
                string[] tmp = GetValue(key).Split(new char[] { separador });
                return new ArrayList(tmp);
            }
        }

        public void SetValueA(string key, ArrayList values)
        {
            if (!ValidateKey(key)) throw new ConfigException("La clave contiene palabras reservadas: " + key);
            SetValueA(key, values, false, limite);
        }

        public void SetValueA(string key, ArrayList values, int limit)
        {
            if (!ValidateKey(key)) throw new ConfigException("La clave contiene palabras reservadas: " + key);
            SetValueA(key, values, false,limit);
        }
        
        private void SetValueA(string key, ArrayList values, bool isinternal, int limit)
        {
            RemoveKey(key);
            if (values.Count > limit)
            {
                int nentradas = values.Count / limit;
                if ((values.Count % limit) != 0) nentradas++;
                SetValue(key+".count",nentradas.ToString(),true);
                
                ArrayList auxvalues = (ArrayList)values.Clone();
                for (int i = 0; i < nentradas; i++)
                {
                    if (auxvalues.Count > limit)
                    {
                        SetValueA(key + ".part" + i, auxvalues.GetRange(0, limit),true,limit);
                        auxvalues.RemoveRange(0, limit);
                    }
                    else
                    {
                        SetValueA(key + ".part" + i, auxvalues, true,limit);
                    }
                }
            } 
            else 
            {
                string tmp = "";
                foreach (object s in values)
                {
                    string new_s = s.ToString();
                    tmp += new_s + separador;
                }
                if (tmp.Length > 0)
                    tmp = tmp.Substring(0, tmp.Length - 1);
                SetValue(key, tmp, true);
            }
        }

        private void RemoveKey(string key)
        {
            if (ExistsKey(key))
                DeleteKey(key); // borrar la normal

            List<string> tmp = new List<string>();

            foreach (string valor in cfg.Keys)
                if (valor.StartsWith(key + "."))
                    tmp.Add(valor);

            foreach (string valor in tmp)
                DeleteKey(valor);
        }

        public void PrependValueA(string key, string value)
        {
            if (!ExistsKey(key))
                SetValue(key, value);
            else
            {
                ArrayList tmp = GetValueA(key);
                if (tmp.Contains(value)) tmp.Remove(value);
                tmp.Insert(0, value);
                SetValueA(key, tmp, true, limite);
            }
        }

        public void AppendValueA(string key, string value)
        {
            if (!ExistsKey(key))
                SetValue(key, value);
            else
            {

                ArrayList tmp = GetValueA(key);
                if (tmp.Contains(value)) tmp.Remove(value);
                tmp.Add(value);
                SetValueA(key, tmp,true,limite);
            }
        }

        public void TrimKey(string key, int max)
        {
            if (!ExistsKey(key)) return;
            ArrayList tmp = GetValueA(key);
            if (tmp.Count<max) return;

            SetValueA(key,tmp.GetRange(0, max));
        }
    }
}
