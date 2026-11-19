# Original test prose dedicated to the public domain (CC0). No book or voice downloads.
$ErrorActionPreference = 'Stop'
$sampleRoot = Join-Path $PSScriptRoot 'sample'
New-Item -ItemType Directory -Force "$sampleRoot/audio" | Out-Null
$text = @'
The morning train arrived beside the river just as the sun appeared over the hills. Clara stepped onto the platform with a small blue bag and a letter from her brother. She had never visited this town before, but the quiet streets already seemed familiar. A baker was opening his shop across the road. The smell of fresh bread drifted through the cool air.
Clara asked the baker for directions to the old lighthouse. He pointed toward a narrow path that followed the water. She bought a loaf of bread and began to walk. Along the way she passed a wooden bridge, a garden full of yellow flowers, and three children watching a fishing boat. The captain waved as the boat moved slowly toward the sea.
At the lighthouse, her brother was waiting beside the door. He smiled and took the bag from her hand. They climbed the winding stairs together and looked out across the bay. For a long time neither of them spoke. Below them the town was waking, and another train was crossing the river. Clara opened the letter once more, then folded it carefully and placed it in her pocket. She knew that she had finally come home.
'@
Add-Type -AssemblyName System.Speech
$voice = New-Object System.Speech.Synthesis.SpeechSynthesizer
$voice.SetOutputToWaveFile("$sampleRoot/audio/000000.wav")
$voice.Speak($text)
$voice.Dispose()
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
$epubPath = "$sampleRoot/book.epub"
if (Test-Path $epubPath) { Remove-Item -LiteralPath $epubPath }
$zip = [IO.Compression.ZipFile]::Open($epubPath, [IO.Compression.ZipArchiveMode]::Create)
$entries = @{
 'mimetype' = 'application/epub+zip'
 'META-INF/container.xml' = '<container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container"><rootfiles><rootfile full-path="content.opf" media-type="application/oebps-package+xml"/></rootfiles></container>'
 'content.opf' = '<package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="id"><metadata xmlns:dc="http://purl.org/dc/elements/1.1/"><dc:identifier id="id">urn:audibly:sample</dc:identifier><dc:title>The River Visit</dc:title><dc:language>en</dc:language><meta property="dcterms:modified">2026-11-05T00:00:00Z</meta></metadata><manifest><item id="chapter" href="chapter.xhtml" media-type="application/xhtml+xml"/><item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav"/></manifest><spine><itemref idref="chapter"/></spine></package>'
 'chapter.xhtml' = '<html xmlns="http://www.w3.org/1999/xhtml"><head><title>The River Visit</title></head><body><section><h1>The River Visit</h1>' + (($text -split "`n" | ForEach-Object { '<p>' + [Security.SecurityElement]::Escape($_.Trim()) + '</p>' }) -join '') + '</section></body></html>'
 'nav.xhtml' = '<html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops"><head><title>Contents</title></head><body><nav epub:type="toc"><ol><li><a href="chapter.xhtml">The River Visit</a></li></ol></nav></body></html>'
}
foreach ($name in $entries.Keys) {
 $entry = $zip.CreateEntry($name, [IO.Compression.CompressionLevel]::NoCompression)
 $writer = New-Object IO.StreamWriter($entry.Open(), (New-Object Text.UTF8Encoding($false)))
 $writer.Write($entries[$name]); $writer.Dispose()
}
$zip.Dispose()
Write-Output "Created legal matching sample at $sampleRoot"
