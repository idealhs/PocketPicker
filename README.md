# PocketPicker

_a proxy tool based on TitanWebProxy that save response which you want into local files._

## Usage

```Batch
PocketPicker "template.json" "savePath"
```

template.json is a json file defined rules that which response should be saved.
Key defined request uri match parttern (_Regex supported_), and value defined file name will be saved.
The file should be like this:  

```json
{
    "www.baidu.com": "baiduResponseFile",
    "cat-match.easygame2021.com": "ylgy",
    "search?q=*": "searchResult"
}
```

At first run, a new CA will be generate and you need add it to OS or firefox CA stroage. The tool will set itself as default system proxy. System proxy setting will be clean atfer tool exit.
