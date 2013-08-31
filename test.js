function clog() { console.log(arguments[0]); }
clog('[Reflection] hi there from "' + Assembly.GetExecutingAssembly().FullName + "'");
clog('[Date] current time is ' + (new Date()).toDateString());
clog('[Array] an array looks like ' + (Array('a','b','c')));
clog('[Function] a function looks like ' + (function(a,b) { clog(a + b); }));
clog('[Math] PI is ' + Math.PI);
clog('[Stdlib] decode uri: (want !) ' + decodeURI('%21'));
var a = {'x':3, 'y':2, 'toString':function() { return '[' + this.x + ',' + this.y + ']'; }};
var a_str;
switch (a.toString()) {
	case '[3,2]': a_str = 'case correct'; break;
	default: a_str = 'case wrong'; break;
}
clog(a_str + ': a = ' + a.toString());
