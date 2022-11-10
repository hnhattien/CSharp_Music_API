const glob = require('glob');
let arr = "Hello";
glob("*.mp3",(err,matchs)=>{
   arr = matchs
   console.log(matchs);
})
