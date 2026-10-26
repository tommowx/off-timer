# Off-Timer

Off-Timer è un'app Windows leggera e portatile per scegliere quando spegnere, riavviare o sospendere il computer. Interfaccia minimale in stile Apple, timer modificabile e annullamento a portata di mano.

## Avvio

Su Windows 10/11, fai doppio clic su `Off-Timer.exe`. Non richiede installazione, connessione Internet o SDK: usa le librerie WPF incluse in Windows.

## Uso

- Inserisci giorni, ore, minuti e secondi; scegli l'azione prima di avviare il timer.
- La durata ammessa è da un secondo a dieci anni. Giorni: 0–3650; ore: 0–23; minuti e secondi: 0–59.
- Off-Timer mostra un riepilogo e chiede conferma; ricorda di salvare il lavoro aperto, perché Windows chiuderà le app alla scadenza.
- Puoi annullare il conto in qualunque momento. L'app deve rimanere aperta; chiuderla o riavviare Windows interrompe il timer.
- La sospensione dipende dal supporto e dalle impostazioni di risparmio energetico del computer.

## Compilazione e verifiche

Codice C# WPF, compatibile con il compilatore .NET Framework presente in Windows. Il controllo integrato non attiva mai le azioni di sistema:

```powershell
Off-Timer.exe --verify
```

Licenza MIT.
