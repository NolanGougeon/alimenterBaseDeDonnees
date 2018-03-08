using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;

namespace alimenterBaseDeDonnees
{
    public partial class Traitement : Form
    {
        public Traitement()
        {
            InitializeComponent();
        }

        private void btParcourir_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog parcourir = new FolderBrowserDialog();
            parcourir.ShowDialog();
            tbRepertoireFichiers.Text = parcourir.SelectedPath;
            string[] filepaths = Directory.GetFiles(parcourir.SelectedPath, "*.csv", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < filepaths.Length; i++)
            {
                clbFichierCsv.Items.Add(filepaths[i].Replace(parcourir.SelectedPath + "\\", ""));
            }
        }

        private void Traitement_Load(object sender, EventArgs e)
        {
            string sConnexion;
            MySqlConnection connexion;
            sConnexion = "user=admin;password=siojjr;database=rallyelecture;host=172.16.0.159";
            connexion = new MySqlConnection(sConnexion);
            connexion.Open();
            string sSelectClasse = "select classe.id,niveauScolaire,anneeScolaire from niveau inner join classe on niveau.id=classe.idNiveau;";
            MySqlCommand SelectClasse = new MySqlCommand(sSelectClasse, connexion);
            MySqlDataReader reader = SelectClasse.ExecuteReader();
            while (reader.Read())
            {
                cbClasse.Items.Add(reader["id"]+" "+reader["niveauScolaire"]+" "+reader["anneeScolaire"]);
            }
            reader.Close();
            connexion.Close();
            renameAuteur();
        }

        private void btIntegration_Click(object sender, EventArgs e)
        {
            string classeNiveau = Convert.ToString(cbClasse.SelectedItem);
            string[] test = classeNiveau.Split(' ');
            bool remiseABlanc = false;
            string sConnexion;
            MySqlConnection connexion;
            sConnexion = "user=admin;password=siojjr;database=rallyelecture;host=172.16.0.159";
            connexion = new MySqlConnection(sConnexion);
            connexion.Open();
            if (cbSuppression.Checked == true)
            {
                try
                {
                    
                    MySqlCommand DeleteAauthUsersInGroups = new MySqlCommand("delete from aauth_user_to_group where user_id in(select idAuth from eleve where idClasse=@classe)",connexion);
                    DeleteAauthUsersInGroups.Parameters.AddWithValue("@classe", test[0]);
                    DeleteAauthUsersInGroups.ExecuteNonQuery();
                    MySqlCommand DeleteAauthUsers = new MySqlCommand("delete from aauth_users where id in (select idauth from eleve where idclasse=@classe)",connexion);
                    DeleteAauthUsers.Parameters.AddWithValue("@classe",test[0]);
                    DeleteAauthUsers.ExecuteNonQuery();
                    string sSupprimerEleve = "delete from eleve where idClasse in(select classe.id from Classe inner join niveau on niveau.id=classe.idNiveau where anneeScolaire=@annee and niveauScolaire=@niveau);";
                    MySqlCommand supprimerEleve = new MySqlCommand(sSupprimerEleve, connexion);
                    supprimerEleve.Parameters.AddWithValue("@annee", test[2]);
                    supprimerEleve.Parameters.AddWithValue("@niveau",test[1]);
                    supprimerEleve.ExecuteNonQuery();
                    
                    remiseABlanc = true;
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message);
                }
                
            }
            

            String[] lines = System.IO.File.ReadAllLines(tbRepertoireFichiers.Text+"\\"+clbFichierCsv.SelectedItem.ToString());
            foreach (string  line in lines)
            {
                string[] csvlecture = line.Split(';');
                string nom = csvlecture[1];
                string prenom = csvlecture[2];
                string mail = csvlecture[3];
                MySqlCommand InsertUsers = new MySqlCommand("Insert into aauth_users(email,pass) values (@email,@pass)", connexion);
                InsertUsers.Parameters.AddWithValue("@email", mail);
                InsertUsers.Parameters.AddWithValue("@pass",GetSha256FromString("siojjr"));
                InsertUsers.ExecuteNonQuery();
                MySqlCommand selectUsers = new MySqlCommand("select id from aauth_users where email=@email",connexion);
                selectUsers.Parameters.AddWithValue("@email", mail);
                MySqlDataReader rdr= selectUsers.ExecuteReader();
                rdr.Read();
                int idAuth = Convert.ToInt32(rdr["id"]);
                rdr.Close();
                MySqlCommand InsertEleves = new MySqlCommand("insert into eleve(nom,prenom,login,idClasse,idAuth) values (@nom,@prenom,@login,@idClasse,@idAuth)",connexion);
                InsertEleves.Parameters.AddWithValue("@nom", nom);
                InsertEleves.Parameters.AddWithValue("idClasse", test[0]);
                InsertEleves.Parameters.AddWithValue("@login", mail);
                InsertEleves.Parameters.AddWithValue("@prenom", prenom);
                InsertEleves.Parameters.AddWithValue("@idAuth",idAuth);
                InsertEleves.ExecuteNonQuery();

                MySqlCommand InsertUserGroup = new MySqlCommand("insert into aauth_user_to_group(user_id,group_id) values(@user_id,@group_id)",connexion);
                InsertUserGroup.Parameters.AddWithValue("@user_id", idAuth);
                InsertUserGroup.Parameters.AddWithValue("@group_id",4);
                InsertUserGroup.ExecuteNonQuery();
            }
            connexion.Close();
        }

        public static string GetSha256FromString(string data)
        {
            var message = Encoding.ASCII.GetBytes(data);
            SHA256Managed hashString = new SHA256Managed();
            string hex = "";
            var hashValue = hashString.ComputeHash(message);
            foreach (byte x in hashValue)
            {
                hex += String.Format("{0:x2}", x);
            }
            return hex;
        }

        public void renameAuteur()
        {
            string sConnexion;
            MySqlConnection connexion;
            sConnexion = "user=admin;password=siojjr;database=rallyelecture;host=172.16.0.159";
            connexion = new MySqlConnection(sConnexion);
            connexion.Open();
            MySqlCommand selectAuteur = new MySqlCommand("select * from auteur", connexion);
            MySqlDataReader rdr = selectAuteur.ExecuteReader();
            while (rdr.Read())
            {
                string nom = Convert.ToString(rdr["nom"]);
                string nouveauNom = "";
                if (nom[0]=='d' && nom[1]=='e' && nom[2]==' ')
                {  
                    for (int i = 3; i < nom.Length; i++)
                    {
                        nouveauNom += nom[i];
                    }
                    MySqlCommand updateAuteur = new MySqlCommand("update auteur set nom=@nom where id=@id", connexion);
                    updateAuteur.Parameters.AddWithValue("@nom", nouveauNom);
                    updateAuteur.Parameters.AddWithValue("@id", Convert.ToInt32(rdr["id"]));
                }
            }
            connexion.Close();
        }
    }
}
