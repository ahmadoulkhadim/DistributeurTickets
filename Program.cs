namespace DistributeurTickets;

class Program
{
    // Un client qui a pris un ticket
    record Client(string Ticket, string Type, string Compte, string Nom, string Prenom);

    // Fichier des compteurs, créé dans le dossier temp du système
    static readonly string CheminFichier = Path.Combine(Path.GetTempPath(), "fnuméro.txt");

    // Texte du message créé UNE seule fois :
    // {0} = numéro du ticket, {1} = nombre de personnes qui attendent avant le client
    const string ModeleMessage = "Votre numéro est {0}, et il y a {1} personne(s) qui attendent avant vous.";

    // Message pour le premier client d'un type (personne n'attend avant lui)
    const string ModelePremier = "Votre numéro est {0}, vous êtes le premier de la file : passez directement au guichet.";

    static void Main(string[] args)
    {
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch (Exception) { }

        List<Client> clients = new List<Client>();

        // À chaque exécution, la banque vient d'ouvrir : tous les compteurs repartent à 0
        try
        {
            InitialiserFichier();
        }
        catch (Exception e)
        {
            Console.WriteLine("Impossible de créer le fichier des numéros : " + e.Message);
            return;
        }

        Console.WriteLine("===== Bienvenue à la banque =====");
        Console.WriteLine("Fichier des numéros : " + CheminFichier);

        bool continuer = true;
        while (continuer)
        {
            try
            {
                Client? client = PrendreTicket();
                if (client is null)
                {
                    break;   // l'utilisateur a tapé Q
                }

                clients.Add(client);
                continuer = DemanderAutreClient();
            }
            catch (IOException e)
            {
                Console.WriteLine("Erreur : problème avec le fichier des numéros.");
                Console.WriteLine("Message système -> " + e.Message);
                continuer = false;
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine("Erreur : accès refusé au fichier des numéros.");
                Console.WriteLine("Message système -> " + e.Message);
                continuer = false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Erreur : le fichier des numéros est illisible ou une erreur inattendue s'est produite.");
                Console.WriteLine("Message système -> " + e.Message);
                continuer = false;
            }
        }

        AfficherClients(clients);
    }

    // ---------------------------------------------------------------
    // Prend les informations du client et lui donne un ticket.
    // Renvoie null si l'utilisateur tape Q.
    // ---------------------------------------------------------------
    static Client? PrendreTicket()
    {
        Console.WriteLine();

        string? choix = Demander("Type d'opération : V-Versement, R-Retrait, I-Informations", VerifierType);
        if (choix is null) return null;

        string? compte = Demander("Numéro de compte", saisie => null);   // aucun contrôle sur le numéro de compte
        if (compte is null) return null;

        string? nom = Demander("Nom", VerifierNom);
        if (nom is null) return null;

        string? prenom = Demander("Prénom", VerifierNom);
        if (prenom is null) return null;

        char lettre = char.ToUpper(choix[0]);
        int rang = ProchainNumero(lettre);       // lit le fichier, ajoute 1, réécrit le fichier
        string ticket = $"{lettre}-{rang}";
        int enAttente = rang - 1;                // personnes du même type arrivées avant

        Console.WriteLine();
        if (enAttente == 0)
        {
            Console.WriteLine(string.Format(ModelePremier, ticket));
        }
        else
        {
            Console.WriteLine(string.Format(ModeleMessage, ticket, enAttente));
        }

        return new Client(ticket, NomDuType(lettre), compte, nom, prenom);
    }

    // Pose une question, vérifie la réponse et redemande tant qu'elle est invalide.
    // Renvoie null si l'utilisateur tape Q (ou si l'entrée est fermée).
    static string? Demander(string question, Func<string, string?> verifier)
    {
        while (true)
        {
            Console.Write(question + " (Q pour quitter) : ");
            string? saisie = Console.ReadLine();

            if (saisie is null)
            {
                return null;
            }

            saisie = saisie.Trim();
            if (saisie.Equals("Q", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string? erreur = verifier(saisie);
            if (erreur is null)
            {
                return saisie;
            }
            Console.WriteLine("Erreur : " + erreur);
        }
    }

    static bool DemanderAutreClient()
    {
        while (true)
        {
            Console.Write("Un autre client ? (O/N, Q pour quitter) : ");
            string? reponse = Console.ReadLine();

            if (reponse is null)
            {
                return false;
            }

            reponse = reponse.Trim().ToUpper();
            if (reponse == "O") return true;
            if (reponse == "N" || reponse == "Q") return false;

            Console.WriteLine("Erreur : répondez par O, N ou Q.");
        }
    }

    // ---------------------------------------------------------------
    // Vérifications des saisies (renvoient null si OK, sinon le message d'erreur)
    // ---------------------------------------------------------------
    static string? VerifierType(string saisie)
    {
        string s = saisie.ToUpper();
        if (s == "V" || s == "R" || s == "I")
        {
            return null;
        }
        return "choisissez V (Versement), R (Retrait), I (Informations) ou Q.";
    }

    static string? VerifierNom(string saisie)
    {
        if (saisie == "")
        {
            return "ce champ ne peut pas être vide.";
        }
        foreach (char c in saisie)
        {
            if (!char.IsLetter(c) && c != ' ' && c != '-' && c != '\'')
            {
                return "saisissez uniquement des lettres (pas de chiffres ni de symboles).";
            }
        }
        return null;
    }

    static string NomDuType(char lettre)
    {
        switch (lettre)
        {
            case 'V': return "Versement";
            case 'R': return "Retrait";
            default: return "Informations";
        }
    }

    // ---------------------------------------------------------------
    // Fichier fnuméro.txt : une ligne par type, par exemple V=3
    // ---------------------------------------------------------------
    static void InitialiserFichier()
    {
        File.WriteAllLines(CheminFichier, new string[] { "V=0", "R=0", "I=0" });
    }

    static Dictionary<char, int> LireCompteurs()
    {
        Dictionary<char, int> compteurs = new Dictionary<char, int>();

        foreach (string ligne in File.ReadAllLines(CheminFichier))
        {
            if (ligne.Trim() == "")
            {
                continue;
            }
            string[] parties = ligne.Split('=');
            compteurs[parties[0][0]] = int.Parse(parties[1]);
        }
        return compteurs;
    }

    static void EcrireCompteurs(Dictionary<char, int> compteurs)
    {
        List<string> lignes = new List<string>();
        foreach (KeyValuePair<char, int> paire in compteurs)
        {
            lignes.Add(paire.Key + "=" + paire.Value);
        }
        File.WriteAllLines(CheminFichier, lignes);
    }

    // Donne le numéro suivant pour un type et le sauvegarde dans le fichier
    static int ProchainNumero(char lettre)
    {
        Dictionary<char, int> compteurs = LireCompteurs();
        compteurs[lettre] = compteurs[lettre] + 1;
        EcrireCompteurs(compteurs);
        return compteurs[lettre];
    }

    // ---------------------------------------------------------------
    // Liste de tous les clients à la fin du programme
    // ---------------------------------------------------------------
    static void AfficherClients(List<Client> clients)
    {
        Console.WriteLine();
        Console.WriteLine("===== Liste des clients =====");

        if (clients.Count == 0)
        {
            Console.WriteLine("Aucun client n'a pris de ticket.");
            return;
        }

        foreach (Client c in clients)
        {
            Console.WriteLine($"{c.Ticket} | {c.Type} | Compte {c.Compte} | {c.Prenom} {c.Nom}");
        }
        Console.WriteLine($"Total : {clients.Count} client(s).");
    }
}