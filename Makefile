SOURCES=jsact.cs jsarray.cs jsbool.cs jscompcon.cs jsdate.cs jsdefs.cs jsexec.cs \
	jsexeccon.cs jsfun.cs jsmath.cs jsnative.cs jsnode.cs jsnumber.cs \
	jsobject.cs jsparse.cs jsref.cs jsstdlib.cs jsstmt.cs jstoken.cs \
	jstokenizer.cs main.cs

js.exe: $(SOURCES)
	mcs -out:$@ $(SOURCES)

clean:
	rm js.exe