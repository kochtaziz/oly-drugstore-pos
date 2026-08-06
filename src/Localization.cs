using System.Collections.Generic;

namespace OlyDrugstorePOS
{
    public static class Localization
    {
        public static string Language = "FR";

        private static readonly Dictionary<string, string> Fr = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> En = new Dictionary<string, string>();

        static Localization()
        {
            Fr["Login"] = "Connexion";
            Fr["Username"] = "Utilisateur";
            Fr["Password"] = "Mot de passe";
            Fr["SignIn"] = "Entrer";
            Fr["InvalidLogin"] = "Identifiants incorrects";
            Fr["Products"] = "Produits";
            Fr["Sales"] = "Vente";
            Fr["Cash"] = "Caisse";
            Fr["Reports"] = "Rapports";
            Fr["Settings"] = "Parametres";
            Fr["Search"] = "Scanner ou rechercher";
            Fr["Add"] = "Ajouter";
            Fr["Remove"] = "Retirer";
            Fr["Increase"] = "+ Quantite";
            Fr["Decrease"] = "- Quantite";
            Fr["ClearCart"] = "Vider ticket";
            Fr["Checkout"] = "Encaisser";
            Fr["PrintTicket"] = "Imprimer";
            Fr["Name"] = "Nom";
            Fr["Category"] = "Categorie";
            Fr["Barcode"] = "Code a barres";
            Fr["PurchasePrice"] = "Prix d'achat";
            Fr["SalePrice"] = "Prix de vente";
            Fr["Tax"] = "Taxe";
            Fr["Quantity"] = "Quantite";
            Fr["Minimum"] = "Minimum";
            Fr["Expiry"] = "Expiration";
            Fr["SaveProduct"] = "Enregistrer produit";
            Fr["DeleteProduct"] = "Supprimer produit";
            Fr["OpenShift"] = "Ouvrir caisse";
            Fr["CloseShift"] = "Fermer caisse";
            Fr["Withdrawal"] = "Sortie de caisse";
            Fr["Deposit"] = "Entree de caisse";
            Fr["Reason"] = "Raison";
            Fr["CountedCash"] = "Argent compte";
            Fr["BankDeposit"] = "Depot banque";
            Fr["Difference"] = "Ecart";
            Fr["Total"] = "Total";
            Fr["Discount"] = "Remise";
            Fr["EmployeeDiscount"] = "Remise employe";
            Fr["Payment"] = "Paiement";
            Fr["Return"] = "Retour";
            Fr["Debt"] = "Dette client";
            Fr["Customer"] = "Client";
            Fr["LowStock"] = "Stock faible";
            Fr["BackupDone"] = "Sauvegarde effectuee";

            En["Login"] = "Login";
            En["Username"] = "Username";
            En["Password"] = "Password";
            En["SignIn"] = "Sign in";
            En["InvalidLogin"] = "Invalid login";
            En["Products"] = "Products";
            En["Sales"] = "Sale";
            En["Cash"] = "Cash";
            En["Reports"] = "Reports";
            En["Settings"] = "Settings";
            En["Search"] = "Scan or search";
            En["Add"] = "Add";
            En["Remove"] = "Remove";
            En["Increase"] = "+ Quantity";
            En["Decrease"] = "- Quantity";
            En["ClearCart"] = "Clear ticket";
            En["Checkout"] = "Checkout";
            En["PrintTicket"] = "Print";
            En["Name"] = "Name";
            En["Category"] = "Category";
            En["Barcode"] = "Barcode";
            En["PurchasePrice"] = "Purchase price";
            En["SalePrice"] = "Sale price";
            En["Tax"] = "Tax";
            En["Quantity"] = "Quantity";
            En["Minimum"] = "Minimum";
            En["Expiry"] = "Expiry";
            En["SaveProduct"] = "Save product";
            En["DeleteProduct"] = "Delete product";
            En["OpenShift"] = "Open shift";
            En["CloseShift"] = "Close shift";
            En["Withdrawal"] = "Withdrawal";
            En["Deposit"] = "Deposit";
            En["Reason"] = "Reason";
            En["CountedCash"] = "Counted cash";
            En["BankDeposit"] = "Bank deposit";
            En["Difference"] = "Difference";
            En["Total"] = "Total";
            En["Discount"] = "Discount";
            En["EmployeeDiscount"] = "Employee discount";
            En["Payment"] = "Payment";
            En["Return"] = "Return";
            En["Debt"] = "Customer debt";
            En["Customer"] = "Customer";
            En["LowStock"] = "Low stock";
            En["BackupDone"] = "Backup completed";
        }

        public static string T(string key)
        {
            Dictionary<string, string> dictionary = Language == "EN" ? En : Fr;
            return dictionary.ContainsKey(key) ? dictionary[key] : key;
        }
    }
}
