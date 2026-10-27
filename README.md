# Off-Timer

Off-Timer è un'app Windows leggera e portatile per scegliere quando spegnere, riavviare o sospendere il computer. L'interfaccia è essenziale e funzionale; include timer modificabile, annullamento, modalità a schermo intero, tema nero e schermata nera.

## Avvio

Su Windows 10/11, fai doppio clic su `Off-Timer.exe`. Non richiede installazione, connessione Internet o SDK: usa le librerie WPF incluse in Windows.

Scarica l'eseguibile: [Off-Timer.exe](https://github.com/tommowx/off-timer/raw/refs/heads/main/dist/Off-Timer.exe).

## Uso

- Inserisci giorni, ore, minuti e secondi; scegli l'azione prima di avviare il timer.
- La durata ammessa è da un secondo a dieci anni. Giorni: 0–3650; ore: 0–23; minuti e secondi: 0–59.
- Off-Timer mostra un riepilogo e chiede conferma; ricorda di salvare il lavoro aperto, perché Windows chiuderà le app alla scadenza.
- Quando parte, mostra il conto alla rovescia aggiornato in un riquadro fisso sopra il modulo e anche nel titolo della finestra; il timer resta visibile mentre scorri.
- Puoi annullare il conto in qualunque momento. L'app deve rimanere aperta; chiuderla o riavviare Windows interrompe il timer.
- Premi **Schermo intero** o **F11** per riempire il display; premi **Esc** per tornare alla finestra.
- **Tema nero** porta a nero uniforme tutta la finestra, comprese le aree vuote; lo stesso pulsante ripristina il tema chiaro.
- **Schermo nero** copre il display principale con una schermata nera. Premi **Esc** o fai clic per tornare all'app; il timer continua a funzionare.
- La sospensione dipende dal supporto e dalle impostazioni di risparmio energetico del computer.

L'icona Windows è in `assets/off-timer.ico`; il master PNG è in `assets/off-timer-icon.png`.

## Compilazione e verifiche

Codice C# WPF, compatibile con il compilatore .NET Framework presente in Windows. Il controllo integrato non attiva mai le azioni di sistema:

```powershell
Off-Timer.exe --verify
```

Licenza MIT.
