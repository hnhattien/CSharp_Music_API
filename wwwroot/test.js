const glob = require("glob")
let arr = "";
glob("/home/sunwarder/Desktop/CrawlMusicData/mp3/*.mp3",(err,matchs)=>{
    matchs = matchs.map(el=>{
        return el.split("/")[el.split("/").length-1]
    })
    console.log(matchs)
})
