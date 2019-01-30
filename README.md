# NfcReader
A simple library that provides to use rfid card readers.

# Usage
Basic usage of the library are provided. I recommend you using events which are very helpful.

## Connection

```csharp
//Initializing
NFCReader NFC = new NFCReader();

//Connecting
NFC.Connect(); // public bool Connect()

//Disconnecting
NFC.Disconnect(); // public void Disconnect()
```
```csharp
//Inserted Event 
NFC.CardInserted += new NFCReader.CardEventHandler(...Some function);

//Ejected Event
NFC.CardEjected += new NFCReader.CardEventHandler(... Some function);

//Enabling Event Watching
NFC.Watch(); //public void Watch()
```

## Read, Write Authorize

```csharp
//Authorizing(which is done automatically by the read and write functions)
NFC.AuthBlock("2"); // private bool AuthBlock(String block)

//Reading
NFC.ReadBlock("2"); //public byte[] ReadBlock(String Block)

//Writing   
NFC.WriteBlock("Some string data that will not exceed block limit", "2"); //public bool WriteBlock(String Text, String Block)
```
## ReaderList, CardUID

```csharp
//Card UID
NFC.GetCardUID();

//Available Readers 
NFC.GetReadersList(); //public List<string> GetReadersList()
```

## Example Inserted and Ejected Event Usage
```csharp
public void Card_Inserted()
{
  try
  {
    if (NFC.Connect())
    {
        //Do stuff like NFC.GetCardUID(); ...
    }
    else
    {
        //Give error message about connection...
    }
  }
  catch (Exception ex)
  {
    this.SetStatusText("Hata: Bir Sorun Oluştu Tekrar Deneyiniz",false);
  }
}
```

```csharp
public void Card_Ejected()
{
   //Do stuff...
   NFC.Disconnect();
}
```
