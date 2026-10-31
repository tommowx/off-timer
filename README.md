# Off-Timer

App Windows portatile per programmare lo spegnimento, il riavvio o la sospensione. Include tema nero uniforme, schermo intero, schermata nera, preset rapidi e preferenze salvate localmente.

## Avvio

Su Windows 10/11, avvia `Off-Timer.exe`. Non richiede installazione o connessione Internet.

[Scarica l'ultima versione](https://github.com/tommowx/off-timer/raw/refs/heads/main/dist/Off-Timer.exe).

## Uso

- Imposta giorni, ore, minuti e secondi oppure scegli un preset; seleziona l'azione e avvia.
- Durata da un secondo a dieci anni. Le preferenze restano sul PC in `%LOCALAPPDATA%\OffTimer`.
- Il riepilogo richiede conferma prima dell'avvio. Il timer usa un contatore monotono e continua anche se cambia l'orologio di Windows.
- Puoi annullare il timer; se chiudi la finestra mentre è attivo, Off-Timer chiede conferma e annulla il conto solo se scegli di chiudere.
- Per impostazione predefinita Windows prova a chiudere normalmente le app. L'opzione di forzatura è esplicita e può far perdere dati non salvati.
- `F11` attiva lo schermo intero; `Esc` esce. Dalla schermata nera si esce con `Esc` o clic.
- La sospensione dipende dal supporto e dalle impostazioni di risparmio energetico del computer.

## Verifica

Il controllo integrato non esegue mai spegnimento, riavvio o sospensione: `Off-Timer.exe --verify`.

Licenza MIT.

