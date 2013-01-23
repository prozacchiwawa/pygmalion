SOURCES=jsact.cs jsarray.cs jsbool.cs jscompcon.cs jsdate.cs jsdefs.cs jsexec.cs \
	jsexeccon.cs jsfun.cs jsmath.cs jsnative.cs jsnode.cs jsnumber.cs \
	jsobject.cs jsparse.cs jsref.cs jsstdlib.cs jsstmt.cs jstoken.cs \
	jstokenizer.cs

all: js.exe

pygmalion.dll: $(SOURCES)
	mcs -target:library -out:$@ $(SOURCES)

js.exe: $(SOURCES) pygmalion.dll
	mcs -out:$@ -r:pygmalion.dll main.cs

clean:
	rm js.exe pygmalion.dll
