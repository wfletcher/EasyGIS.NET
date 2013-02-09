
/****************************************************************************
**
** Copyright (C) 2008 - 2011 Winston Fletcher.
** All rights reserved.
**
** This file is part of the EGIS.Web.controls class library of Easy GIS .NET.
** 
** Easy GIS .NET is free software: you can redistribute it and/or modify
** it under the terms of the GNU Lesser General Public License version 3 as
** published by the Free Software Foundation and appearing in the file
** lgpl-license.txt included in the packaging of this file.
**
** Easy GIS .NET is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License and
** GNU Lesser General Public License along with Easy GIS .NET.
** If not, see <http://www.gnu.org/licenses/>.
**
****************************************************************************/




// YAHOO Javascript

/*
Copyright (c) 2009, Yahoo! Inc. All rights reserved.
Code licensed under the BSD License:
http://developer.yahoo.net/yui/license.txt
version: 2.7.0
*/

if(typeof YAHOO=="undefined"||!YAHOO){var YAHOO={};}YAHOO.namespace=function(){var A=arguments,E=null,C,B,D;for(C=0;C<A.length;C=C+1){D=(""+A[C]).split(".");E=YAHOO;for(B=(D[0]=="YAHOO")?1:0;B<D.length;B=B+1){E[D[B]]=E[D[B]]||{};E=E[D[B]];}}return E;};YAHOO.log=function(D,A,C){var B=YAHOO.widget.Logger;if(B&&B.log){return B.log(D,A,C);}else{return false;}};YAHOO.register=function(A,E,D){var I=YAHOO.env.modules,B,H,G,F,C;if(!I[A]){I[A]={versions:[],builds:[]};}B=I[A];H=D.version;G=D.build;F=YAHOO.env.listeners;B.name=A;B.version=H;B.build=G;B.versions.push(H);B.builds.push(G);B.mainClass=E;for(C=0;C<F.length;C=C+1){F[C](B);}if(E){E.VERSION=H;E.BUILD=G;}else{YAHOO.log("mainClass is undefined for module "+A,"warn");}};YAHOO.env=YAHOO.env||{modules:[],listeners:[]};YAHOO.env.getVersion=function(A){return YAHOO.env.modules[A]||null;};YAHOO.env.ua=function(){var C={ie:0,opera:0,gecko:0,webkit:0,mobile:null,air:0,caja:0},B=navigator.userAgent,A;if((/KHTML/).test(B)){C.webkit=1;}A=B.match(/AppleWebKit\/([^\s]*)/);if(A&&A[1]){C.webkit=parseFloat(A[1]);if(/ Mobile\//.test(B)){C.mobile="Apple";}else{A=B.match(/NokiaN[^\/]*/);if(A){C.mobile=A[0];}}A=B.match(/AdobeAIR\/([^\s]*)/);if(A){C.air=A[0];}}if(!C.webkit){A=B.match(/Opera[\s\/]([^\s]*)/);if(A&&A[1]){C.opera=parseFloat(A[1]);A=B.match(/Opera Mini[^;]*/);if(A){C.mobile=A[0];}}else{A=B.match(/MSIE\s([^;]*)/);if(A&&A[1]){C.ie=parseFloat(A[1]);}else{A=B.match(/Gecko\/([^\s]*)/);if(A){C.gecko=1;A=B.match(/rv:([^\s\)]*)/);if(A&&A[1]){C.gecko=parseFloat(A[1]);}}}}}A=B.match(/Caja\/([^\s]*)/);if(A&&A[1]){C.caja=parseFloat(A[1]);}return C;}();(function(){YAHOO.namespace("util","widget","example");if("undefined"!==typeof YAHOO_config){var B=YAHOO_config.listener,A=YAHOO.env.listeners,D=true,C;if(B){for(C=0;C<A.length;C=C+1){if(A[C]==B){D=false;break;}}if(D){A.push(B);}}}})();YAHOO.lang=YAHOO.lang||{};(function(){var B=YAHOO.lang,F="[object Array]",C="[object Function]",A=Object.prototype,E=["toString","valueOf"],D={isArray:function(G){return A.toString.apply(G)===F;},isBoolean:function(G){return typeof G==="boolean";},isFunction:function(G){return A.toString.apply(G)===C;},isNull:function(G){return G===null;},isNumber:function(G){return typeof G==="number"&&isFinite(G);},isObject:function(G){return(G&&(typeof G==="object"||B.isFunction(G)))||false;},isString:function(G){return typeof G==="string";},isUndefined:function(G){return typeof G==="undefined";},_IEEnumFix:(YAHOO.env.ua.ie)?function(I,H){var G,K,J;for(G=0;G<E.length;G=G+1){K=E[G];J=H[K];if(B.isFunction(J)&&J!=A[K]){I[K]=J;}}}:function(){},extend:function(J,K,I){if(!K||!J){throw new Error("extend failed, please check that "+"all dependencies are included.");}var H=function(){},G;H.prototype=K.prototype;J.prototype=new H();J.prototype.constructor=J;J.superclass=K.prototype;if(K.prototype.constructor==A.constructor){K.prototype.constructor=K;}if(I){for(G in I){if(B.hasOwnProperty(I,G)){J.prototype[G]=I[G];}}B._IEEnumFix(J.prototype,I);}},augmentObject:function(K,J){if(!J||!K){throw new Error("Absorb failed, verify dependencies.");}var G=arguments,I,L,H=G[2];if(H&&H!==true){for(I=2;I<G.length;I=I+1){K[G[I]]=J[G[I]];}}else{for(L in J){if(H||!(L in K)){K[L]=J[L];}}B._IEEnumFix(K,J);}},augmentProto:function(J,I){if(!I||!J){throw new Error("Augment failed, verify dependencies.");}var G=[J.prototype,I.prototype],H;for(H=2;H<arguments.length;H=H+1){G.push(arguments[H]);}B.augmentObject.apply(this,G);},dump:function(G,L){var I,K,N=[],O="{...}",H="f(){...}",M=", ",J=" => ";if(!B.isObject(G)){return G+"";}else{if(G instanceof Date||("nodeType" in G&&"tagName" in G)){return G;}else{if(B.isFunction(G)){return H;}}}L=(B.isNumber(L))?L:3;if(B.isArray(G)){N.push("[");for(I=0,K=G.length;I<K;I=I+1){if(B.isObject(G[I])){N.push((L>0)?B.dump(G[I],L-1):O);}else{N.push(G[I]);}N.push(M);}if(N.length>1){N.pop();}N.push("]");}else{N.push("{");for(I in G){if(B.hasOwnProperty(G,I)){N.push(I+J);if(B.isObject(G[I])){N.push((L>0)?B.dump(G[I],L-1):O);}else{N.push(G[I]);}N.push(M);}}if(N.length>1){N.pop();}N.push("}");}return N.join("");},substitute:function(V,H,O){var L,K,J,R,S,U,Q=[],I,M="dump",P=" ",G="{",T="}",N;for(;;){L=V.lastIndexOf(G);if(L<0){break;}K=V.indexOf(T,L);if(L+1>=K){break;}I=V.substring(L+1,K);R=I;U=null;J=R.indexOf(P);if(J>-1){U=R.substring(J+1);R=R.substring(0,J);}S=H[R];if(O){S=O(R,S,U);}if(B.isObject(S)){if(B.isArray(S)){S=B.dump(S,parseInt(U,10));}else{U=U||"";N=U.indexOf(M);if(N>-1){U=U.substring(4);}if(S.toString===A.toString||N>-1){S=B.dump(S,parseInt(U,10));}else{S=S.toString();}}}else{if(!B.isString(S)&&!B.isNumber(S)){S="~-"+Q.length+"-~";Q[Q.length]=I;}}V=V.substring(0,L)+S+V.substring(K+1);}for(L=Q.length-1;L>=0;L=L-1){V=V.replace(new RegExp("~-"+L+"-~"),"{"+Q[L]+"}","g");}return V;},trim:function(G){try{return G.replace(/^\s+|\s+$/g,"");}catch(H){return G;}},merge:function(){var J={},H=arguments,G=H.length,I;for(I=0;I<G;I=I+1){B.augmentObject(J,H[I],true);}return J;},later:function(N,H,O,J,K){N=N||0;H=H||{};var I=O,M=J,L,G;if(B.isString(O)){I=H[O];}if(!I){throw new TypeError("method undefined");}if(!B.isArray(M)){M=[J];}L=function(){I.apply(H,M);};G=(K)?setInterval(L,N):setTimeout(L,N);return{interval:K,cancel:function(){if(this.interval){clearInterval(G);}else{clearTimeout(G);}}};},isValue:function(G){return(B.isObject(G)||B.isString(G)||B.isNumber(G)||B.isBoolean(G));}};B.hasOwnProperty=(A.hasOwnProperty)?function(G,H){return G&&G.hasOwnProperty(H);}:function(G,H){return !B.isUndefined(G[H])&&G.constructor.prototype[H]!==G[H];};D.augmentObject(B,D,true);YAHOO.util.Lang=B;B.augment=B.augmentProto;YAHOO.augment=B.augmentProto;YAHOO.extend=B.extend;})();YAHOO.register("yahoo",YAHOO,{version:"2.7.0",build:"1799"});


/*
Copyright (c) 2009, Yahoo! Inc. All rights reserved.
Code licensed under the BSD License:
http://developer.yahoo.net/yui/license.txt
version: 2.7.0
*/
YAHOO.util.CustomEvent=function(D,C,B,A){this.type=D;this.scope=C||window;this.silent=B;this.signature=A||YAHOO.util.CustomEvent.LIST;this.subscribers=[];if(!this.silent){}var E="_YUICEOnSubscribe";if(D!==E){this.subscribeEvent=new YAHOO.util.CustomEvent(E,this,true);}this.lastError=null;};YAHOO.util.CustomEvent.LIST=0;YAHOO.util.CustomEvent.FLAT=1;YAHOO.util.CustomEvent.prototype={subscribe:function(A,B,C){if(!A){throw new Error("Invalid callback for subscriber to '"+this.type+"'");}if(this.subscribeEvent){this.subscribeEvent.fire(A,B,C);}this.subscribers.push(new YAHOO.util.Subscriber(A,B,C));},unsubscribe:function(D,F){if(!D){return this.unsubscribeAll();}var E=false;for(var B=0,A=this.subscribers.length;B<A;++B){var C=this.subscribers[B];if(C&&C.contains(D,F)){this._delete(B);E=true;}}return E;},fire:function(){this.lastError=null;var K=[],E=this.subscribers.length;if(!E&&this.silent){return true;}var I=[].slice.call(arguments,0),G=true,D,J=false;if(!this.silent){}var C=this.subscribers.slice(),A=YAHOO.util.Event.throwErrors;for(D=0;D<E;++D){var M=C[D];if(!M){J=true;}else{if(!this.silent){}var L=M.getScope(this.scope);if(this.signature==YAHOO.util.CustomEvent.FLAT){var B=null;if(I.length>0){B=I[0];}try{G=M.fn.call(L,B,M.obj);}catch(F){this.lastError=F;if(A){throw F;}}}else{try{G=M.fn.call(L,this.type,I,M.obj);}catch(H){this.lastError=H;if(A){throw H;}}}if(false===G){if(!this.silent){}break;}}}return(G!==false);},unsubscribeAll:function(){var A=this.subscribers.length,B;for(B=A-1;B>-1;B--){this._delete(B);}this.subscribers=[];return A;},_delete:function(A){var B=this.subscribers[A];if(B){delete B.fn;delete B.obj;}this.subscribers.splice(A,1);},toString:function(){return"CustomEvent: "+"'"+this.type+"', "+"context: "+this.scope;}};YAHOO.util.Subscriber=function(A,B,C){this.fn=A;this.obj=YAHOO.lang.isUndefined(B)?null:B;this.overrideContext=C;};YAHOO.util.Subscriber.prototype.getScope=function(A){if(this.overrideContext){if(this.overrideContext===true){return this.obj;}else{return this.overrideContext;}}return A;};YAHOO.util.Subscriber.prototype.contains=function(A,B){if(B){return(this.fn==A&&this.obj==B);}else{return(this.fn==A);}};YAHOO.util.Subscriber.prototype.toString=function(){return"Subscriber { obj: "+this.obj+", overrideContext: "+(this.overrideContext||"no")+" }";};if(!YAHOO.util.Event){YAHOO.util.Event=function(){var H=false;var I=[];var J=[];var G=[];var E=[];var C=0;var F=[];var B=[];var A=0;var D={63232:38,63233:40,63234:37,63235:39,63276:33,63277:34,25:9};var K=YAHOO.env.ua.ie?"focusin":"focus";var L=YAHOO.env.ua.ie?"focusout":"blur";return{POLL_RETRYS:2000,POLL_INTERVAL:20,EL:0,TYPE:1,FN:2,WFN:3,UNLOAD_OBJ:3,ADJ_SCOPE:4,OBJ:5,OVERRIDE:6,lastError:null,isSafari:YAHOO.env.ua.webkit,webkit:YAHOO.env.ua.webkit,isIE:YAHOO.env.ua.ie,_interval:null,_dri:null,DOMReady:false,throwErrors:false,startInterval:function(){if(!this._interval){var M=this;var N=function(){M._tryPreloadAttach();};this._interval=setInterval(N,this.POLL_INTERVAL);}},onAvailable:function(S,O,Q,R,P){var M=(YAHOO.lang.isString(S))?[S]:S;for(var N=0;N<M.length;N=N+1){F.push({id:M[N],fn:O,obj:Q,overrideContext:R,checkReady:P});}C=this.POLL_RETRYS;this.startInterval();},onContentReady:function(P,M,N,O){this.onAvailable(P,M,N,O,true);},onDOMReady:function(M,N,O){if(this.DOMReady){setTimeout(function(){var P=window;if(O){if(O===true){P=N;}else{P=O;}}M.call(P,"DOMReady",[],N);},0);}else{this.DOMReadyEvent.subscribe(M,N,O);}},_addListener:function(O,M,Y,S,W,b){if(!Y||!Y.call){return false;}if(this._isValidCollection(O)){var Z=true;for(var T=0,V=O.length;T<V;++T){Z=this.on(O[T],M,Y,S,W)&&Z;}return Z;}else{if(YAHOO.lang.isString(O)){var R=this.getEl(O);if(R){O=R;}else{this.onAvailable(O,function(){YAHOO.util.Event.on(O,M,Y,S,W);});return true;}}}if(!O){return false;}if("unload"==M&&S!==this){J[J.length]=[O,M,Y,S,W];return true;}var N=O;if(W){if(W===true){N=S;}else{N=W;}}var P=function(c){return Y.call(N,YAHOO.util.Event.getEvent(c,O),S);};var a=[O,M,Y,P,N,S,W];var U=I.length;I[U]=a;if(this.useLegacyEvent(O,M)){var Q=this.getLegacyIndex(O,M);if(Q==-1||O!=G[Q][0]){Q=G.length;B[O.id+M]=Q;G[Q]=[O,M,O["on"+M]];E[Q]=[];O["on"+M]=function(c){YAHOO.util.Event.fireLegacyEvent(YAHOO.util.Event.getEvent(c),Q);};}E[Q].push(a);}else{try{this._simpleAdd(O,M,P,b);}catch(X){this.lastError=X;this.removeListener(O,M,Y);return false;}}return true;},addListener:function(N,Q,M,O,P){return this._addListener(N,Q,M,O,P,false);},addFocusListener:function(N,M,O,P){return this._addListener(N,K,M,O,P,true);},removeFocusListener:function(N,M){return this.removeListener(N,K,M);},addBlurListener:function(N,M,O,P){return this._addListener(N,L,M,O,P,true);},removeBlurListener:function(N,M){return this.removeListener(N,L,M);},fireLegacyEvent:function(R,P){var T=true,M,V,U,N,S;V=E[P].slice();for(var O=0,Q=V.length;O<Q;++O){U=V[O];if(U&&U[this.WFN]){N=U[this.ADJ_SCOPE];S=U[this.WFN].call(N,R);T=(T&&S);}}M=G[P];if(M&&M[2]){M[2](R);}return T;},getLegacyIndex:function(N,O){var M=this.generateId(N)+O;if(typeof B[M]=="undefined"){return -1;}else{return B[M];}},useLegacyEvent:function(M,N){return(this.webkit&&this.webkit<419&&("click"==N||"dblclick"==N));},removeListener:function(N,M,V){var Q,T,X;if(typeof N=="string"){N=this.getEl(N);}else{if(this._isValidCollection(N)){var W=true;for(Q=N.length-1;Q>-1;Q--){W=(this.removeListener(N[Q],M,V)&&W);}return W;}}if(!V||!V.call){return this.purgeElement(N,false,M);}if("unload"==M){for(Q=J.length-1;Q>-1;Q--){X=J[Q];if(X&&X[0]==N&&X[1]==M&&X[2]==V){J.splice(Q,1);return true;}}return false;}var R=null;var S=arguments[3];if("undefined"===typeof S){S=this._getCacheIndex(N,M,V);}if(S>=0){R=I[S];}if(!N||!R){return false;}if(this.useLegacyEvent(N,M)){var P=this.getLegacyIndex(N,M);var O=E[P];if(O){for(Q=0,T=O.length;Q<T;++Q){X=O[Q];if(X&&X[this.EL]==N&&X[this.TYPE]==M&&X[this.FN]==V){O.splice(Q,1);break;}}}}else{try{this._simpleRemove(N,M,R[this.WFN],false);}catch(U){this.lastError=U;return false;}}delete I[S][this.WFN];delete I[S][this.FN];
I.splice(S,1);return true;},getTarget:function(O,N){var M=O.target||O.srcElement;return this.resolveTextNode(M);},resolveTextNode:function(N){try{if(N&&3==N.nodeType){return N.parentNode;}}catch(M){}return N;},getPageX:function(N){var M=N.pageX;if(!M&&0!==M){M=N.clientX||0;if(this.isIE){M+=this._getScrollLeft();}}return M;},getPageY:function(M){var N=M.pageY;if(!N&&0!==N){N=M.clientY||0;if(this.isIE){N+=this._getScrollTop();}}return N;},getXY:function(M){return[this.getPageX(M),this.getPageY(M)];},getRelatedTarget:function(N){var M=N.relatedTarget;if(!M){if(N.type=="mouseout"){M=N.toElement;}else{if(N.type=="mouseover"){M=N.fromElement;}}}return this.resolveTextNode(M);},getTime:function(O){if(!O.time){var N=new Date().getTime();try{O.time=N;}catch(M){this.lastError=M;return N;}}return O.time;},stopEvent:function(M){this.stopPropagation(M);this.preventDefault(M);},stopPropagation:function(M){if(M.stopPropagation){M.stopPropagation();}else{M.cancelBubble=true;}},preventDefault:function(M){if(M.preventDefault){M.preventDefault();}else{M.returnValue=false;}},getEvent:function(O,M){var N=O||window.event;if(!N){var P=this.getEvent.caller;while(P){N=P.arguments[0];if(N&&Event==N.constructor){break;}P=P.caller;}}return N;},getCharCode:function(N){var M=N.keyCode||N.charCode||0;if(YAHOO.env.ua.webkit&&(M in D)){M=D[M];}return M;},_getCacheIndex:function(Q,R,P){for(var O=0,N=I.length;O<N;O=O+1){var M=I[O];if(M&&M[this.FN]==P&&M[this.EL]==Q&&M[this.TYPE]==R){return O;}}return -1;},generateId:function(M){var N=M.id;if(!N){N="yuievtautoid-"+A;++A;M.id=N;}return N;},_isValidCollection:function(N){try{return(N&&typeof N!=="string"&&N.length&&!N.tagName&&!N.alert&&typeof N[0]!=="undefined");}catch(M){return false;}},elCache:{},getEl:function(M){return(typeof M==="string")?document.getElementById(M):M;},clearCache:function(){},DOMReadyEvent:new YAHOO.util.CustomEvent("DOMReady",this),_load:function(N){if(!H){H=true;var M=YAHOO.util.Event;M._ready();M._tryPreloadAttach();}},_ready:function(N){var M=YAHOO.util.Event;if(!M.DOMReady){M.DOMReady=true;M.DOMReadyEvent.fire();M._simpleRemove(document,"DOMContentLoaded",M._ready);}},_tryPreloadAttach:function(){if(F.length===0){C=0;if(this._interval){clearInterval(this._interval);this._interval=null;}return;}if(this.locked){return;}if(this.isIE){if(!this.DOMReady){this.startInterval();return;}}this.locked=true;var S=!H;if(!S){S=(C>0&&F.length>0);}var R=[];var T=function(V,W){var U=V;if(W.overrideContext){if(W.overrideContext===true){U=W.obj;}else{U=W.overrideContext;}}W.fn.call(U,W.obj);};var N,M,Q,P,O=[];for(N=0,M=F.length;N<M;N=N+1){Q=F[N];if(Q){P=this.getEl(Q.id);if(P){if(Q.checkReady){if(H||P.nextSibling||!S){O.push(Q);F[N]=null;}}else{T(P,Q);F[N]=null;}}else{R.push(Q);}}}for(N=0,M=O.length;N<M;N=N+1){Q=O[N];T(this.getEl(Q.id),Q);}C--;if(S){for(N=F.length-1;N>-1;N--){Q=F[N];if(!Q||!Q.id){F.splice(N,1);}}this.startInterval();}else{if(this._interval){clearInterval(this._interval);this._interval=null;}}this.locked=false;},purgeElement:function(Q,R,T){var O=(YAHOO.lang.isString(Q))?this.getEl(Q):Q;var S=this.getListeners(O,T),P,M;if(S){for(P=S.length-1;P>-1;P--){var N=S[P];this.removeListener(O,N.type,N.fn);}}if(R&&O&&O.childNodes){for(P=0,M=O.childNodes.length;P<M;++P){this.purgeElement(O.childNodes[P],R,T);}}},getListeners:function(O,M){var R=[],N;if(!M){N=[I,J];}else{if(M==="unload"){N=[J];}else{N=[I];}}var T=(YAHOO.lang.isString(O))?this.getEl(O):O;for(var Q=0;Q<N.length;Q=Q+1){var V=N[Q];if(V){for(var S=0,U=V.length;S<U;++S){var P=V[S];if(P&&P[this.EL]===T&&(!M||M===P[this.TYPE])){R.push({type:P[this.TYPE],fn:P[this.FN],obj:P[this.OBJ],adjust:P[this.OVERRIDE],scope:P[this.ADJ_SCOPE],index:S});}}}}return(R.length)?R:null;},_unload:function(T){var N=YAHOO.util.Event,Q,P,O,S,R,U=J.slice(),M;for(Q=0,S=J.length;Q<S;++Q){O=U[Q];if(O){M=window;if(O[N.ADJ_SCOPE]){if(O[N.ADJ_SCOPE]===true){M=O[N.UNLOAD_OBJ];}else{M=O[N.ADJ_SCOPE];}}O[N.FN].call(M,N.getEvent(T,O[N.EL]),O[N.UNLOAD_OBJ]);U[Q]=null;}}O=null;M=null;J=null;if(I){for(P=I.length-1;P>-1;P--){O=I[P];if(O){N.removeListener(O[N.EL],O[N.TYPE],O[N.FN],P);}}O=null;}G=null;N._simpleRemove(window,"unload",N._unload);},_getScrollLeft:function(){return this._getScroll()[1];},_getScrollTop:function(){return this._getScroll()[0];},_getScroll:function(){var M=document.documentElement,N=document.body;if(M&&(M.scrollTop||M.scrollLeft)){return[M.scrollTop,M.scrollLeft];}else{if(N){return[N.scrollTop,N.scrollLeft];}else{return[0,0];}}},regCE:function(){},_simpleAdd:function(){if(window.addEventListener){return function(O,P,N,M){O.addEventListener(P,N,(M));};}else{if(window.attachEvent){return function(O,P,N,M){O.attachEvent("on"+P,N);};}else{return function(){};}}}(),_simpleRemove:function(){if(window.removeEventListener){return function(O,P,N,M){O.removeEventListener(P,N,(M));};}else{if(window.detachEvent){return function(N,O,M){N.detachEvent("on"+O,M);};}else{return function(){};}}}()};}();(function(){var EU=YAHOO.util.Event;EU.on=EU.addListener;EU.onFocus=EU.addFocusListener;EU.onBlur=EU.addBlurListener;
/* DOMReady: based on work by: Dean Edwards/John Resig/Matthias Miller */
if(EU.isIE){YAHOO.util.Event.onDOMReady(YAHOO.util.Event._tryPreloadAttach,YAHOO.util.Event,true);var n=document.createElement("p");EU._dri=setInterval(function(){try{n.doScroll("left");clearInterval(EU._dri);EU._dri=null;EU._ready();n=null;}catch(ex){}},EU.POLL_INTERVAL);}else{if(EU.webkit&&EU.webkit<525){EU._dri=setInterval(function(){var rs=document.readyState;if("loaded"==rs||"complete"==rs){clearInterval(EU._dri);EU._dri=null;EU._ready();}},EU.POLL_INTERVAL);}else{EU._simpleAdd(document,"DOMContentLoaded",EU._ready);}}EU._simpleAdd(window,"load",EU._load);EU._simpleAdd(window,"unload",EU._unload);EU._tryPreloadAttach();})();}YAHOO.util.EventProvider=function(){};YAHOO.util.EventProvider.prototype={__yui_events:null,__yui_subscribers:null,subscribe:function(A,C,F,E){this.__yui_events=this.__yui_events||{};var D=this.__yui_events[A];if(D){D.subscribe(C,F,E);
}else{this.__yui_subscribers=this.__yui_subscribers||{};var B=this.__yui_subscribers;if(!B[A]){B[A]=[];}B[A].push({fn:C,obj:F,overrideContext:E});}},unsubscribe:function(C,E,G){this.__yui_events=this.__yui_events||{};var A=this.__yui_events;if(C){var F=A[C];if(F){return F.unsubscribe(E,G);}}else{var B=true;for(var D in A){if(YAHOO.lang.hasOwnProperty(A,D)){B=B&&A[D].unsubscribe(E,G);}}return B;}return false;},unsubscribeAll:function(A){return this.unsubscribe(A);},createEvent:function(G,D){this.__yui_events=this.__yui_events||{};var A=D||{};var I=this.__yui_events;if(I[G]){}else{var H=A.scope||this;var E=(A.silent);var B=new YAHOO.util.CustomEvent(G,H,E,YAHOO.util.CustomEvent.FLAT);I[G]=B;if(A.onSubscribeCallback){B.subscribeEvent.subscribe(A.onSubscribeCallback);}this.__yui_subscribers=this.__yui_subscribers||{};var F=this.__yui_subscribers[G];if(F){for(var C=0;C<F.length;++C){B.subscribe(F[C].fn,F[C].obj,F[C].overrideContext);}}}return I[G];},fireEvent:function(E,D,A,C){this.__yui_events=this.__yui_events||{};var G=this.__yui_events[E];if(!G){return null;}var B=[];for(var F=1;F<arguments.length;++F){B.push(arguments[F]);}return G.fire.apply(G,B);},hasEvent:function(A){if(this.__yui_events){if(this.__yui_events[A]){return true;}}return false;}};(function(){var A=YAHOO.util.Event,C=YAHOO.lang;YAHOO.util.KeyListener=function(D,I,E,F){if(!D){}else{if(!I){}else{if(!E){}}}if(!F){F=YAHOO.util.KeyListener.KEYDOWN;}var G=new YAHOO.util.CustomEvent("keyPressed");this.enabledEvent=new YAHOO.util.CustomEvent("enabled");this.disabledEvent=new YAHOO.util.CustomEvent("disabled");if(C.isString(D)){D=document.getElementById(D);}if(C.isFunction(E)){G.subscribe(E);}else{G.subscribe(E.fn,E.scope,E.correctScope);}function H(O,N){if(!I.shift){I.shift=false;}if(!I.alt){I.alt=false;}if(!I.ctrl){I.ctrl=false;}if(O.shiftKey==I.shift&&O.altKey==I.alt&&O.ctrlKey==I.ctrl){var J,M=I.keys,L;if(YAHOO.lang.isArray(M)){for(var K=0;K<M.length;K++){J=M[K];L=A.getCharCode(O);if(J==L){G.fire(L,O);break;}}}else{L=A.getCharCode(O);if(M==L){G.fire(L,O);}}}}this.enable=function(){if(!this.enabled){A.on(D,F,H);this.enabledEvent.fire(I);}this.enabled=true;};this.disable=function(){if(this.enabled){A.removeListener(D,F,H);this.disabledEvent.fire(I);}this.enabled=false;};this.toString=function(){return"KeyListener ["+I.keys+"] "+D.tagName+(D.id?"["+D.id+"]":"");};};var B=YAHOO.util.KeyListener;B.KEYDOWN="keydown";B.KEYUP="keyup";B.KEY={ALT:18,BACK_SPACE:8,CAPS_LOCK:20,CONTROL:17,DELETE:46,DOWN:40,END:35,ENTER:13,ESCAPE:27,HOME:36,LEFT:37,META:224,NUM_LOCK:144,PAGE_DOWN:34,PAGE_UP:33,PAUSE:19,PRINTSCREEN:44,RIGHT:39,SCROLL_LOCK:145,SHIFT:16,SPACE:32,TAB:9,UP:38};})();YAHOO.register("event",YAHOO.util.Event,{version:"2.7.0",build:"1799"});

// END YAHOO Javascriipt



var egis = new function () {

    function Debug(msg) {

        //        if (window.console) {
        //            if (msg != null) {
        //                window.console.log(msg.toString());
        //            }
        //        }
    };

    var _mapObject = null;

    this.GetMap = function () {
        return _mapObject;
    };


    function DocGetElementsByClassName(re, tag, cn) {
        var _5 = (tag == "*" && re.all) ? re.all : re.getElementsByTagName(tag);
        var _6 = new Array();
        cn = cn.replace(/\-/g, "\\-");
        var _7 = new RegExp("(^|\\s)" + cn + "(\\s|$)");
        var _8;
        for (var i = 0; i < _5.length; i++) {
            _8 = _5[i];
            if (_7.test(_8.className)) {
                _6.push(_8);
            }
        }
        return (_6);
    };

    //returns the left,top position of an element
    function findPos(obj) {
        var curleft = curtop = 0;
        if (obj.offsetParent) {
            curleft = obj.offsetLeft;
            curtop = obj.offsetTop;
            while (obj = obj.offsetParent) {
                curleft += obj.offsetLeft;
                curtop += obj.offsetTop;
            }
        }
        return [curleft, curtop];
    };

    function setOpacity(obj, op) {
        obj.style.filter = "alpha(opacity=" + op + ")";
        op = op / 100.0;
        obj.style.opacity = "" + op;
    };


    function GetXmlHttpObject() {
        var xmlHttp = null;
        try {
            // Firefox, Opera 8.0+, Safari
            xmlHttp = new XMLHttpRequest();
        }
        catch (e) {
            // Internet Explorer
            try {
                xmlHttp = new ActiveXObject("Msxml2.XMLHTTP");
            }
            catch (e) {
                xmlHttp = new ActiveXObject("Microsoft.XMLHTTP");
            }
        }
        return xmlHttp;
    };


    function _GisUtil() {
        this.MaxLLMercProjD = 85.0511287798066;
        this.MaxMercY = this.LLToMercator(180, 90)[1];
    };

    _GisUtil.prototype.ZoomLevelToScale = function (zoomLevel) {
        return (256.0 / 360) * (1 << zoomLevel);
    };

    _GisUtil.prototype.ScaleToZoomLevel = function (scale) {
        return Math.round(Math.log(scale * 360 / 256.0) / Math.log(2));
    };

    _GisUtil.prototype.GetTileFromGisLocation = function (lon, lat, zoomLevel) {
        var pix = this.LLToPixel(lat, lon, zoomLevel);
        return [Math.floor(pix[0] / 256), Math.floor(pix[1] / 256)];
    };

    _GisUtil.prototype.GetTileFromMercLocation = function (lon, lat, zoomLevel) {
        var pix = this.MercToPixel(lat, lon, zoomLevel);
        return [Math.floor(pix[0] / 256), Math.floor(pix[1] / 256)];
    };


    _GisUtil.prototype.LLToPixel = function (lat, lon, zoomLevel) {
        //convert LL to Mercatator
        var merc = this.LLToMercator(lon, lat);
        var scale = this.ZoomLevelToScale(zoomLevel);
        var x = Math.round((merc[0] + 180) * scale);
        var y = Math.round((this.MaxMercY - merc[1]) * scale);
        return [x, y];
    };

    _GisUtil.prototype.MercToPixel = function (lat, lon, zoomLevel) {
        //convert LL to Mercatator
        var merc = [lon, lat]; //this.LLToMercator(lon, lat);
        var scale = this.ZoomLevelToScale(zoomLevel);
        var x = Math.round((merc[0] + 180) * scale);
        var y = Math.round((this.MaxMercY - merc[1]) * scale);
        return [x, y];
    };

    _GisUtil.prototype.PixelToMerc = function (pixX, pixY, zoomLevel) {
        var d = 1.0 / this.ZoomLevelToScale(zoomLevel);
        return [(d * pixX) - 180, 180 - (d * pixY)];
    };

    _GisUtil.prototype.LLToMercator = function (x, y) {
        if (y > this.MaxLLMercProjD) {
            y = this.MaxLLMercProjD;
        }
        else if (y < -this.MaxLLMercProjD) {
            y = -this.MaxLLMercProjD;
        }
        var d = (Math.PI / 180) * y;
        var sd = Math.sin(d);
        d = (90 / Math.PI) * Math.log((1 + sd) / (1 - sd));
        return [x, d];
    };

    _GisUtil.prototype.MercProjectionToLL = function (x, y) {
        var d = (Math.PI / 180) * y;
        d = Math.atan(0.5 * (Math.exp(d) - Math.exp(-d)));
        d = d * (180 / Math.PI);
        return [x, d];
    };

    var GisUtil = new _GisUtil();

    this.gisUtil = GisUtil;

    this.panLeft = function () {
        var mapobj = this.GetMap(); // _mapObject;
        if (mapobj != null) {
            mapobj.panLeft();
        }
        return false;
    };

    this.panRight = function () {
        var mapobj = this.GetMap();
        if (mapobj != null) {
            mapobj.panRight();
        }
        return false;
    };

    this.panUp = function () {
        var mapobj = this.GetMap();
        if (mapobj != null) {
            mapobj.panUp();
        }
        return false;
    };

    this.panDown = function () {
        var mapobj = this.GetMap();
        if (mapobj != null) {
            mapobj.panDown();
        }
        return false;
    };

    this.zoomIn = function () {
        var mapobj = this.GetMap();
        Debug(mapobj.Markers);

        if (mapobj != null) {
            mapobj.zoomIn();

        }
        //alert("zoomIn");
        return false;
    };

    this.zoomOut = function () {
        var mapobj = this.GetMap();
        if (mapobj != null) {
            mapobj.zoomOut();
        }
        return false;
    };


    function refreshMap(img, handlerUrl, px, py, z, mapid) {
        img.src = handlerUrl + "?w=" + img.width + "&h=" + img.height + "&x=" + px + "&y=" + py + "&zoom=" + z + "&mapid=" + mapid;
    };

    function MapTile(idX, idY, zoomLevel, backgroundImage) {
        this.IdX = idX;
        this.IdY = idY;
        this.ZoomLevel = zoomLevel;
        this.Img = new Image();
        this.Img.id = "tile_" + this.IdX + "_" + this.IdY + "_" + this.ZoomLevel;
        this.Img.width = "256px";
        this.Img.height = "256px";
        this.Img.style.width = "256px";
        this.Img.style.height = "256px";
        this.Img.style.position = "absolute";
        this.Img.style.visibility = "visible";
        this.Img.style.backgroundColor = "#dedede";
        this.Img.alt = "" + this.Img.id;
        this.LoadingPanel = this.CreateLoadingPanel(backgroundImage);
        this.Img.src = "";
    };

    MapTile.prototype.SetPositionParams = function (idX, idY, zoomLevel) {
        if (this.Idx != idX || this.idY != idY || this.zoomLevel != zoomLevel) {
            this.IdX = idX;
            this.IdY = idY;
            this.ZoomLevel = zoomLevel;
        }
    };

    MapTile.prototype.IsSafari = function () {
        var ua = navigator.userAgent.toLowerCase();
        if (ua.indexOf('safari') >= 0 && ua.indexOf('chrome') < 0) {
            Debug("is safari!");
            return true;
        }
        return false;
    }

    MapTile.prototype.LoadImage = function (handlerUrl, mapId, dcrs) {
        var url = handlerUrl + "?tx=" + this.IdX + "&ty=" + this.IdY + "&zoom=" + this.ZoomLevel + "&mapid=" + encodeURIComponent(mapId) + "&dcrs=" + dcrs + "&version=3";
        Debug(url);
        this.Img.style.visibility = "hidden";
        this.LoadingPanel.style.visibility = "visible";
        if (this.IsSafari()) this.Img.src = ""; //only for safari
        this.Img.src = url;
    };

    MapTile.prototype.CreateLoadingPanel = function (backgroundImage) {
        var pnl = document.createElement("div");
        pnl.className = "tlp";
        pnl.style.width = "256px";
        pnl.style.left = "0px";
        pnl.style.top = "0px";
        pnl.style.height = "256px";
        pnl.style.position = "absolute";
        pnl.style.padding = "0px";
        pnl.style.visibility = "visible";
        pnl.style.backgroundRepeat = "no-repeat";
        pnl.style.overflow = "hidden";
        pnl.style.verticalAlign = "middle";
        pnl.style.backgroundPosition = "center"
        pnl.style.backgroundColor = "#efefde";
        pnl.style.backgroundImage = backgroundImage;
        pnl.style.visibility = "hidden";
        return pnl;
    };

    function AddTileEventHandlers(tile) {
        if (window.addEventListener) {
            tile.Img.addEventListener("load", function () { TileImageLoad(tile); }, false);
        }
        else {
            tile.Img.attachEvent("onload", function () { TileImageLoad(tile); });
        }
    };

    function TileImageLoad(tile) {
        tile.Img.style.visibility = "visible";
        tile.LoadingPanel.style.visibility = "hidden";
    };


    function TileCollection(handlerUrl, mapid, mapcenter, zoomLevel, mapPixWidth, mapPixHeight, evtpnl) {
        this.CountX = Math.ceil(mapPixWidth / 256.0) + 1;
        this.CountY = Math.ceil(mapPixHeight / 256.0) + 1;
        this.HandlerUrl = handlerUrl;
        this.MapId = mapid;
        this.PanOffset = [0, 0];
        this.MapOffset = this.GetTopLeftGisPoint(mapPixWidth, mapPixHeight, mapcenter, GisUtil.ZoomLevelToScale(zoomLevel));
        this.TileOffset = GisUtil.GetTileFromMercLocation(this.MapOffset[0], this.MapOffset[1], zoomLevel);
        this.Tiles = new Array(this.CountX);
        this.MapPixOffset = GisUtil.MercToPixel(this.MapOffset[1], this.MapOffset[0], zoomLevel);
        this.MapPixOffset[0] = (this.TileOffset[0] * 256) - this.MapPixOffset[0];
        this.MapPixOffset[1] = (this.TileOffset[1] * 256) - this.MapPixOffset[1];
        var dx = this.MapPixOffset[0];
        for (var x = 0; x < this.CountX; ++x) {
            this.Tiles[x] = new Array(this.CountY);
            var dy = this.MapPixOffset[1];
            for (var y = 0; y < this.CountY; ++y) {
                this.Tiles[x][y] = this.CreateTile(this.TileOffset[0] + x, this.TileOffset[1] + y, zoomLevel, dx, dy, evtpnl);
                dy += 256;
                this.Tiles[x][y].LoadImage(this.HandlerUrl, this.MapId, "dcrs");
            }
            dx += 256;
        }
    };

    TileCollection.prototype.CreateTile = function (tilex, tiley, zoomLevel, pixOffX, pixOffY, evtpnl) {
        var backgroundImg;
        if (evtpnl.currentStyle) {
            //ie opera
            backgroundImg = evtpnl.currentStyle.backgroundImage;
        }
        else {
            //Firefox
            backgroundImg = getComputedStyle(evtpnl, '').getPropertyValue('background-image');
        }

        var tile = new MapTile(tilex, tiley, zoomLevel, backgroundImg);
        tile.Img.style.left = "" + pixOffX + "px";
        tile.Img.style.top = "" + pixOffY + "px";
        tile.LoadingPanel.style.left = tile.Img.style.left;
        tile.LoadingPanel.style.top = tile.Img.style.top;

        evtpnl.parentNode.insertBefore(tile.Img, evtpnl); //.parentNode.childNodes[1]);
        evtpnl.parentNode.insertBefore(tile.LoadingPanel, evtpnl); //.parentNode.childNodes[1]);
        AddTileEventHandlers(tile);
        return tile;
    };

    TileCollection.prototype.SetPanOffset = function (offx, offy) {
        this.PanOffset[0] = offx;
        this.PanOffset[1] = offy;
        var dx = this.MapPixOffset[0] + offx;
        for (var x = 0; x < this.CountX; ++x) {
            var dy = this.MapPixOffset[1] + offy;
            for (var y = 0; y < this.CountY; ++y) {
                var img = this.Tiles[x][y].Img;
                img.style.left = "" + (dx) + "px";
                img.style.top = "" + (dy) + "px";
                this.Tiles[x][y].LoadingPanel.style.left = img.style.left;
                this.Tiles[x][y].LoadingPanel.style.top = img.style.top;
                dy += 256;
            }
            dx += 256;
        }
    };

    TileCollection.prototype.RefreshTiles = function (mapcenter, zoomLevel, mapPixWidth, mapPixHeight) {

        this.MapOffset = this.GetTopLeftGisPoint(mapPixWidth, mapPixHeight, mapcenter, GisUtil.ZoomLevelToScale(zoomLevel));
        this.TileOffset = GisUtil.GetTileFromMercLocation(this.MapOffset[0], this.MapOffset[1], zoomLevel);
        this.MapPixOffset = GisUtil.MercToPixel(this.MapOffset[1], this.MapOffset[0], zoomLevel);
        this.MapPixOffset[0] = (this.TileOffset[0] * 256) - this.MapPixOffset[0];
        this.MapPixOffset[1] = (this.TileOffset[1] * 256) - this.MapPixOffset[1];
        var dx = this.MapPixOffset[0];
        for (var x = 0; x < this.CountX; ++x) {
            var dy = this.MapPixOffset[1];
            for (var y = 0; y < this.CountY; ++y) {
                var img = this.Tiles[x][y].Img;
                img.style.left = "" + (dx) + "px";
                img.style.top = "" + (dy) + "px";
                this.Tiles[x][y].LoadingPanel.style.left = img.style.left;
                this.Tiles[x][y].LoadingPanel.style.top = img.style.top;
                this.Tiles[x][y].SetPositionParams(this.TileOffset[0] + x, this.TileOffset[1] + y, zoomLevel);
                this.Tiles[x][y].LoadImage(this.HandlerUrl, this.MapId, "dcrs");
                dy += 256;
            }
            dx += 256;
        }
        this.PanOffset[0] = 0;
        this.PanOffset[1] = 0;
    };

    TileCollection.prototype.GetTopLeftGisPoint = function (mapWidth, mapHeight, mapcenter, mapscale) {
        var scale = 1.0 / mapscale;
        var dx = ((mapWidth / 2)) * scale;
        var dy = ((mapHeight / 2)) * scale;
        return [mapcenter[0] - dx, mapcenter[1] + dy];
    };


    function MapObject(handlerUrl, mapid, _lon, _lat, _zoomLevel, evtpnl, dcrs, cacheOnClient) {
        this.handlerUrl = handlerUrl;
        this.mapId = mapid;
        this.lon = _lon;
        this.lat = _lat;
        this.zoomLevel = _zoomLevel;
        this.mouseIsDown = false;
        this.offsetX = 0;
        this.offsetY = 0;
        this.evtpnl = evtpnl;
        this.parentColor = evtpnl.parentNode.style.backgroundColor;
        if (this.parentColor == null) this.parentColor = "";

        this.width = this.evtpnl.style.width.substring(0, this.evtpnl.style.width.length - 2);
        this.height = this.evtpnl.style.height.substring(0, this.evtpnl.style.height.length - 2);
        Debug(this.width);

        this.dcrs = dcrs;
        this.cacheOnClient = cacheOnClient;

        this.MinZoom = Number.MIN_VALUE;
        this.MaxZoom = Number.MAX_VALUE;

        this.ZoomChangedEvent = new YAHOO.util.CustomEvent("ZoomChanged", this);
        this.BoundsChangedEvent = new YAHOO.util.CustomEvent("BoundsChanged", this);
        this.TooltipTimer = setInterval("egis.GlobalTooltipTimer('" + this.evtpnl + "')", 100);
        this.ShowTooltip = false;
        var d = new Date();
        this.ShowTooltipTime = d.getTime() + 2000;
        this.TooltipX = 0;
        this.TooltipY = 0;
        this.TooltipPanel = this.CreateTooltipPanel();
        var bgUrl = this.evtpnl.parentNode.parentNode.childNodes[this.evtpnl.parentNode.parentNode.childNodes.length - 2].value;
        Debug(this.evtpnl.parentNode.parentNode.childNodes[this.evtpnl.parentNode.parentNode.childNodes.length - 2].id);
        this.TooltipPanel.style.backgroundImage = 'url(' + bgUrl + ')';
        this.TooltipPanel.style.backgroundRepeat = "no-repeat";
        this.evtpnl.parentNode.appendChild(this.TooltipPanel);
        this.LastTooltipDisplayTime = d.getTime();
        this.LastKeydownTime = d.getTime();

        this.MapTiles = new TileCollection(handlerUrl, mapid, [1.0 * this.lon, 1.0 * this.lat], this.zoomLevel/*GisUtil.ScaleToZoomLevel(1.0 * this.hfz.value)*/, this.width, this.height, this.evtpnl);
        this.refreshMap();

        Debug(evtpnl.id);
    };


    MapObject.prototype.CreateTooltipPanel = function () {
        var TooltipPanel = document.createElement("div");
        TooltipPanel.className = "sfmaptooltip";
        TooltipPanel.style.width = "190px";
        TooltipPanel.style.left = "-50px";
        TooltipPanel.style.top = "-100px";
        TooltipPanel.style.height = "110px";
        TooltipPanel.style.position = "absolute";
        TooltipPanel.style.padding = "25px";
        TooltipPanel.style.visibility = "hidden";
        TooltipPanel.style.backgroundRepeat = "no-repeat";
        TooltipPanel.style.zIndex = 99999;
        TooltipPanel.style.overflow = "hidden";
        TooltipPanel.style.verticalAlign = "middle";
        return TooltipPanel;
    };

    MapObject.prototype.DisplayTooltip = function (text, x, y) {
        this.TooltipPanel.innerHTML = text;
        this.TooltipPanel.style.left = (x + 5) + "px";
        this.TooltipPanel.style.top = (y + 5) + "px";
        this.TooltipPanel.style.visibility = "visible";
        var d = new Date();
        this.LastTooltipDisplayTime = d.getTime();
    };

    MapObject.prototype.HideTooltip = function () {
        this.TooltipPanel.style.visibility = "hidden";
    };

    this.GlobalTooltipTimer = function (mapid) {
        var mapobj = this.GetMap();
        if (mapobj != null) {
            mapobj.TooltipTimerElapsed();
        }
    };


    MapObject.prototype.TooltipTimerElapsed = function () {
        var d = new Date();
        if (this.ShowTooltip && d.getTime() >= this.ShowTooltipTime) {
            //convert the projected coordinates to lat long
            var ll = GisUtil.MercProjectionToLL(this.TooltipX, this.TooltipY);
            this.SendTooltipRequest(this.handlerUrl, ll[0], ll[1], this.zoomLevel, this.mapId);
        }
    };


    MapObject.prototype.SendTooltipRequest = function (handlerUrl, px, py, zoom, mapId) {
        var AjaxObj = GetXmlHttpObject();
        if (AjaxObj == null) {
            alert("Your browser does not support AJAX!");
            return;
        }
        var url = handlerUrl + "?getshape=true&x=" + px + "&y=" + py + "&zoom=" + zoom + "&mapid=" + encodeURIComponent(mapId) + "&dcrs=" + this.dcrs;

        var mapobj = this;
        AjaxObj.onreadystatechange = function () {
            mapobj.AjaxStateChanged(AjaxObj)
        }
        AjaxObj.open("GET", url, true);
        AjaxObj.send(null);
    };


    MapObject.prototype.AjaxStateChanged = function (AjaxObj) {
        if (AjaxObj == null) return;

        if (AjaxObj.readyState == 4) {
            var lines = AjaxObj.responseText.split('\n');
            if (lines[0] == 'true') {
                var point = lines[1].split(',');
                var x = 1.0 * point[0];
                var y = 1.0 * point[1];
                var mercXY = GisUtil.LLToMercator(x, y);
                var mousePos = this.GisPointToMousePos(mercXY[0], mercXY[1]);
                this.DisplayTooltip(lines[2], mousePos[0], mousePos[1]);
            }
            else {
                this.HideTooltip();
            }
        }
    };

    MapObject.prototype.SetMapCenter = function (lon, lat) {
        var xy = GisUtil.LLToMercator(lon, lat);
        this.lon = xy[0];
        this.lat = xy[1];
        this.refreshMap();
        this.FireBoundsChangedEvent()
    };


    MapObject.prototype.SetMapCenterAndZoomLevel = function (lon, lat, level) {
        var xy = GisUtil.LLToMercator(lon, lat);
        this.lon = xy[0];
        this.lat = xy[1];

        var fireZoomEvent = false;

        if (level > 0 && level < 32) {

            if (level >= this.MinZoom && z <= this.MaxZoom) {
                this.zoomLevel = level;
                fireZoomEvent = true;
            }
        }

        this.refreshMap();
        this.FireBoundsChangedEvent();
        if (fireZoomEvent) this.FireZoomChangedEvent();
    };



    MapObject.prototype.GetMapCenter = function () {
        return GisUtil.MercProjectionToLL(this.lon * 1.0, this.lat * 1.0);
    };

    MapObject.prototype.GetZoomLevel = function () {
        return this.zoomLevel; // GisUtil.ScaleToZoomLevel(1.0 * this.hfz.value);
    };

    MapObject.prototype.SetZoomLevel = function (level) {
        if (level > 0 && level < 32) {
            var z = level; // GisUtil.ZoomLevelToScale(level);
            if (z >= this.MinZoom && z <= this.MaxZoom) {
                this.zoomLevel = z;
                this.refreshMap();
                this.HideTooltip();
                this.FireZoomChangedEvent();
            }
        }
    };


    MapObject.prototype.FireZoomChangedEvent = function () {
        this.ZoomChangedEvent.fire(this.zoomLevel);
    };

    MapObject.prototype.FireBoundsChangedEvent = function () {
        var scale = 1.0 / (1.0 * GisUtil.ZoomLevelToScale(this.zoomLevel));
        var px = (1.0 * this.lon);
        var py = (1.0 * this.lat);
        var w = this.width * scale;
        var h = this.height * scale;
        this.BoundsChangedEvent.fire(px - (w * 0.5), py - (h * 0.50), px + (w * 0.5), py + (h * 0.5));
    };

    MapObject.prototype.restoreParent = function () {
        this.evtpnl.parentNode.style.backgroundColor = this.parentColor;
    };

    MapObject.prototype.dimParent = function () {
        this.evt.parentNode.style.backgroundColor = "#606060";
    };

    MapObject.prototype.toString = function () {
        return "egis.MapObject";
    };

    MapObject.prototype.zoomOut = function () {
        //var z = this.hfz.value * 1;
        //z *= 0.5;
        var z = (this.zoomLevel * 1) - 1;
        if (z >= this.MinZoom) {
            this.zoomLevel = z;
            this.refreshMap();
            this.HideTooltip();
            this.FireZoomChangedEvent();
        }
    };

    MapObject.prototype.zoomIn = function () {
        Debug("zoomLevel = " + this.zoomLevel);
        var z = (this.zoomLevel * 1) + 1; // this.hfz.value * 1;
        //z *= 2;
        if (z <= this.MaxZoom) {
            this.zoomLevel = z;
            Debug("zoomLevel now = " + this.zoomLevel);
            this.refreshMap();
            this.HideTooltip();
            this.FireZoomChangedEvent();
        }
    };

    MapObject.prototype.panLeft = function () {
        var x = this.lon * 1;
        x -= ((this.width / 4) / (GisUtil.ZoomLevelToScale(this.zoomLevel))); //(this.hfz.value));
        this.lon = x;
        this.HideTooltip();
        this.refreshMap();
        this.FireBoundsChangedEvent();
    };

    MapObject.prototype.panRight = function () {
        var x = this.lon * 1;
        x += ((this.width / 4) / (GisUtil.ZoomLevelToScale(this.zoomLevel)));
        this.lon = x;
        this.refreshMap();
        this.HideTooltip();
        this.FireBoundsChangedEvent();
    };

    MapObject.prototype.panUp = function () {
        var y = this.lat;
        y += ((this.height / 4) / (GisUtil.ZoomLevelToScale(this.zoomLevel)));
        this.lat = y;
        this.refreshMap();
        this.HideTooltip();
        this.FireBoundsChangedEvent();
    };

    MapObject.prototype.panDown = function () {
        var y = this.lat;
        y -= ((this.height / 4) / (GisUtil.ZoomLevelToScale(this.zoomLevel))); //(1.0 * this.hfz.value));
        this.lat = y;
        this.refreshMap();
        this.HideTooltip();
        this.FireBoundsChangedEvent();
    };

    MapObject.prototype.MousePosToGisPoint = function (x, y) {
        var scale = 1.0 / (GisUtil.ZoomLevelToScale(this.zoomLevel)); //(1.0 * this.hfz.value);
        var w = this.width;
        var h = this.height;
        var dx = ((w / 2) - 1.0 * x) * scale;
        var dy = ((h / 2) - 1.0 * y) * scale;
        return [(1.0 * this.lon) - dx, (this.lat) + dy];
    };


    MapObject.prototype.GisPointToMousePos = function (x, y) {
        var scale = GisUtil.ZoomLevelToScale(this.zoomLevel); //(1.0 * this.hfz.value);
        var dx = x - (1.0 * this.lon);
        var dy = (this.lat) - y;
        var w = this.width;
        var h = this.height;
        dx = dx * scale;
        dy = dy * scale;
        return [Math.round((w / 2) + dx), Math.round((h / 2) + dy)];
    };

    MapObject.prototype.refreshMap = function () {
        Debug("w:" + this.evtpnl.style.width + ", left:" + this.evtpnl.style.left);
        var left = this.MapTiles.PanOffset[0]; //0;//this.img.style.left;
        var top = this.MapTiles.PanOffset[1]; //this.img.style.top;
        var scale = 1.0 / GisUtil.ZoomLevelToScale(this.zoomLevel); //(1.0 * this.hfz.value);
        var dx = (1.0 * left) * scale;
        var dy = (1.0 * top) * scale;
        var px = (1.0 * this.lon) - dx;
        var py = (1.0 * this.lat) + dy;
        this.lat = py;
        this.lon = px;
        this.MapTiles.RefreshTiles([1.0 * this.lon, 1.0 * this.lat], this.zoomLevel/*GisUtil.ScaleToZoomLevel(1.0 * this.hfz.value)*/, this.width, this.height);
        this.UpdateMarkers();
    };


    function AddEventHandler(elem, eventType, handler) {
        if (elem.addEventListener)
            elem.addEventListener(eventType, handler, false);
        else if (elem.attachEvent)
            elem.attachEvent('on' + eventType, handler);
    };


    MapObject.prototype.AddMarker = function (context) {//markerId, imgUrl, imgWidth, imgHeight, lat, lon) {

        var defaultImgUrl = this.defaultMarkerImage;
        var defaultImgWidth = this.defaultMarkerImageWidth;
        var defaultImgHeight = this.defaultMarkerImageHeight;
        var defaults = {
            imgUrl: defaultImgUrl,
            imgWidth: defaultImgWidth,
            imgHeight: defaultImgHeight,
            clickHandler: '',
            markerId: '',
            lat: '',
            lon: ''

        };
        var context = extend(defaults, context);

        if (context.markerId == '' || context.lat == '' || context.lon == '') {
            alert("AddMarker missing required parameter\nUsage map.AddMarker({ markerId: 'id', lat: yyy, lon: xxx});");
            return null;
        }

        if (this.Markers == null) this.Markers = new Array();
        var mapPin = new Image();
        mapPin.id = context.markerId;
        mapPin.style.position = "absolute";
        mapPin.src = context.imgUrl;
        mapPin.imgWidth = context.imgWidth;
        mapPin.imgHeight = context.imgHeight;
        if (context.clickHandler != '') {
            AddEventHandler(mapPin, 'click', context.clickHandler);
        }
        this.evtpnl.parentNode.appendChild(mapPin);
        mapPin.lat = context.lat;
        mapPin.lon = context.lon;
        this.Markers.push(mapPin);
        this.UpdateMarkers();
        return mapPin;
    };

    MapObject.prototype.SetMarkerOffset = function (offx, offy) {
        if (this.Markers != null) {
            //update the map pin position
            for (var n = 0; n < this.Markers.length; n++) {
                this.Markers[n].style.left = "" + (this.Markers[n].pixcoords[0] + offx) + "px";
                this.Markers[n].style.top = "" + (this.Markers[n].pixcoords[1] + offy) + "px";
            }
        }
    };

    MapObject.prototype.UpdateMarkers = function () {
        if (this.Markers == null || this.mouseIsDown) {
            //alert("mouseIsDown:" + this.mouseIsDown + ",Markers?" + this.Markers);
            return;
        }
        for (var n = 0; n < this.Markers.length; n++) {
            var merc = GisUtil.LLToMercator(this.Markers[n].lon, this.Markers[n].lat);
            this.Markers[n].pixcoords = this.GisPointToMousePos(merc[0], merc[1]);
            this.Markers[n].pixcoords[1] -= this.Markers[n].imgHeight
            this.Markers[n].style.left = "" + (this.Markers[n].pixcoords[0]) + "px";
            this.Markers[n].style.top = "" + (this.Markers[n].pixcoords[1]) + "px";
        }
    };


    function AddMapEventHandlers(mapObj) {
        Debug("attaching events to : " + mapObj.evtpnl.id);
        if (window.addEventListener) {
            mapObj.evtpnl.addEventListener("mousedown", MapMouseDown, true);
            mapObj.evtpnl.addEventListener("mouseup", MapMouseUp, true);
            mapObj.evtpnl.addEventListener("mousemove", MapMouseMove, true);
            mapObj.evtpnl.addEventListener("mouseout", MapMouseOut, false);
            mapObj.evtpnl.addEventListener("DOMMouseScroll", MapMouseWheel, false);
            mapObj.evtpnl.addEventListener("mousewheel", MapMouseWheel, false);
            window.addEventListener("keydown", MapKeyDown, false);

            mapObj.evtpnl.addEventListener('touchmove', MapTouchMove, false);
            mapObj.evtpnl.addEventListener('touchstart', MapTouchStart, false);
            mapObj.evtpnl.addEventListener('touchend', MapTouchEnd, false);



        }
        else {
            mapObj.evtpnl.attachEvent("onmousedown", MapMouseDown);
            mapObj.evtpnl.attachEvent("onmouseup", MapMouseUp);
            document.attachEvent("onmousemove", MapMouseMove);
            mapObj.evtpnl.attachEvent("onmouseout", MapMouseOut);
            mapObj.evtpnl.attachEvent("onmousewheel", MapMouseWheel);
            mapObj.evtpnl.attachEvent("onkeydown", MapKeyDown);
        }
    };


    function GetEventTarget(evt) {
        var target;
        if (evt["srcElement"]) {
            target = evt["srcElement"];
        }
        else {
            target = evt["target"];
        }
        return target;
    };

    function GetMouseOffset(evt, target) {
        var parentPos = findPos(target.parentNode);
        var left = 0;
        var top = 0;
        if (evt.pageX || evt.pageY) {
            left = evt.pageX - parentPos[0];
            top = evt.pageY - parentPos[1];
        }
        else if (evt.clientX || evt.clientY) {
            left = evt.clientX + document.body.scrollLeft + document.documentElement.scrollLeft - parentPos[0];
            top = evt.clientY + document.body.scrollTop + document.documentElement.scrollTop - parentPos[1];
        }
        return [left, top];
    };

    function MapMouseDown(evt) {
        Debug("mouseDown");
        var target = GetEventTarget(evt);
        Debug(target.id);
        var mapobj = egis.GetMap();
        if (mapobj != null) {
            var mousePos = GetMouseOffset(evt, mapobj.evtpnl);
            var left = 0;
            var top = 0;
            mapobj.mouseIsDown = true;
            mapobj.offsetX = mousePos[0] - (1 * left);
            mapobj.offsetY = mousePos[1] - (1 * top);
            mapobj.ShowTooltip = false;
            mapobj.HideTooltip();
        }
        if (evt.preventDefault) {
            evt.preventDefault();
        }
        if (evt.stopPropagation) {
            evt.stopPropagation();
        }
        if (window.event) {
            window.event.cancelBubble = true;
        }
        return false;
    };

    function MapMouseUp(evt) {
        //   var target = GetEventTarget(evt);
        var mapobj = egis.GetMap();
        if (mapobj != null) {
            if (mapobj.mouseIsDown) {
                mapobj.mouseIsDown = false;
                mapobj.refreshMap();
                mapobj.FireBoundsChangedEvent();
                mapobj.ShowTooltip = true;
                var d = new Date();
                mapobj.ShowTooltipTime = d.getTime() + 2000;
            }
        }
        if (evt.preventDefault) {
            evt.preventDefault();
        }
        if (evt.stopPropagation) {
            evt.stopPropagation();
        }
        if (window.event) {
            window.event.cancelBubble = true;
        }
        return false;
    };

    function MapMouseOut(evt) {
        //var target = GetEventTarget(evt);
        var mapobj = egis.GetMap();
        if (mapobj != null) {
            if (mapobj.mouseIsDown) {
                mapobj.mouseIsDown = false;
                mapobj.refreshMap();
                mapobj.FireBoundsChangedEvent();
            }
            mapobj.ShowTooltip = false;
            mapobj.HideTooltip();
        }
        return false;
    };


    var lmx = -1;
    var lmy = -1;

    function MapMouseMove(evt) {
        var mapobj = egis.GetMap();
        if (mapobj != null) {
            var d = new Date();
            if (mapobj.mouseIsDown) {
                var mousePos = GetMouseOffset(evt, mapobj.evtpnl);
                var x = mousePos[0] - mapobj.offsetX;
                var y = mousePos[1] - mapobj.offsetY;
                mapobj.MapTiles.SetPanOffset(x, y);
                mapobj.SetMarkerOffset(x, y);
            }
            else {
                var mousePos = GetMouseOffset(evt, mapobj.evtpnl);
                if (mousePos[0] != lmx && mousePos[1] != lmy) {
                    lmy = mousePos[0];
                    lmx = mousePos[1];
                    mapobj.ShowTooltip = true;
                    mapobj.ShowTooltipTime = d.getTime() + 200;
                    var gisPoint = mapobj.MousePosToGisPoint(mousePos[0], mousePos[1]);
                    mapobj.TooltipX = gisPoint[0];
                    mapobj.TooltipY = gisPoint[1];
                }
            }

            if (d.getTime() > (mapobj.LastTooltipDisplayTime + 500)) {
                mapobj.HideTooltip();
            }
        }
        if (evt.preventDefault) {
            evt.preventDefault();
        }
        if (evt.stopPropagation) {
            evt.stopPropagation();
        }
        if (window.event) {
            window.event.cancelBubble = true;
        }
        return false;
    };

    function MapMouseWheel(evt) {
        var mapobj = egis.GetMap();
        if (mapobj != null) {
            var wheelData = evt.detail ? evt.detail * -1 : evt.wheelDelta / 40;
            if (wheelData > 0) {
                mapobj.zoomIn();
            }
            else {
                mapobj.zoomOut();
            }
        }
        return false;
    };



    function MapKeyDown(event) {
        var mapobj = egis.GetMap();

        var d = new Date();
        if (d.getTime() - mapobj.LastKeydownTime > 100) {
            mapobj.LastKeydownTime = d.getTime();
            if (event.keyCode == 38) {
                mapobj.panUp();
            }
            else if (event.keyCode == 40) {
                mapobj.panDown();
            }
            else if (event.keyCode == 37) {
                mapobj.panLeft();
            }
            else if (event.keyCode == 39) {
                mapobj.panRight();
            }
        }
        if (event.keyCode >= 37 && event.keyCode <= 40) {
            if (event.preventDefault) {
                event.preventDefault();
            }
            if (event.stopPropagation) {
                event.stopPropagation();
            }
            if (window.event) {
                window.event.cancelBubble = true;
            }
        }
        return false;
    };



    // touch events

    function MapTouchStart(evt) {
        // If there's exactly one finger inside this element
        if (event.targetTouches.length == 1) {
            var touch = event.targetTouches[0];

            var mapobj = egis.GetMap();
            if (mapobj != null) {
                var mousePos = GetMouseOffset(touch, mapobj.evtpnl);
                //var left = 0;
                //var top = 0;
                mapobj.mouseIsDown = true;
                mapobj.offsetX = mousePos[0]; // -(1 * left);
                mapobj.offsetY = mousePos[1]; // -(1 * top);
                mapobj.ShowTooltip = false;
                mapobj.HideTooltip();
            }
        }
        if (evt.preventDefault) {
            evt.preventDefault();
        }
        if (evt.stopPropagation) {
            evt.stopPropagation();
        }
        if (window.event) {
            window.event.cancelBubble = true;
        }
        return false;
    };

    function MapTouchEnd(evt) {
        if (event.targetTouches.length == 0) {
            var mapobj = egis.GetMap();
            if (mapobj != null) {
                if (mapobj.mouseIsDown) {
                    mapobj.mouseIsDown = false;
                    mapobj.refreshMap();
                    mapobj.FireBoundsChangedEvent();
                    mapobj.ShowTooltip = true;
                }
            }
        }
        if (evt.preventDefault) {
            evt.preventDefault();
        }
        if (evt.stopPropagation) {
            evt.stopPropagation();
        }
        if (window.event) {
            window.event.cancelBubble = true;
        }
        return false;
    };

    function MapTouchMove(evt) {
        if (event.targetTouches.length == 1) {
            var touch = event.targetTouches[0];

            var mapobj = egis.GetMap();
            if (mapobj != null) {
                var d = new Date();
                if (mapobj.mouseIsDown) {
                    var mousePos = GetMouseOffset(touch, mapobj.evtpnl);
                    var x = mousePos[0] - mapobj.offsetX;
                    var y = mousePos[1] - mapobj.offsetY;
                    mapobj.MapTiles.SetPanOffset(x, y);
                    mapobj.SetMarkerOffset(x, y);
                }
            }
        }
        if (evt.preventDefault) {
            evt.preventDefault();
        }
        if (evt.stopPropagation) {
            evt.stopPropagation();
        }
        if (window.event) {
            window.event.cancelBubble = true;
        }
        return false;
    };


    function LoadingImageLoad(mapObj) {
        //   mapObj.img.src = mapObj.loadingImage.src;                                
    };


    this.initMapping = function (handlerUrl, lon, lat, zoomLevel, mapid, epid, dcrs, cacheOnClient, minz, maxz) {

        var evtpnl = document.getElementById(epid);
        if (_mapObject == null) {
            //var hfcoc = document.getElementById(coc);
            _mapObject = new MapObject(handlerUrl, mapid, lon, lat, zoomLevel, evtpnl, dcrs, cacheOnClient);
            AddMapEventHandlers(_mapObject);
            _mapObject.MinZoom = minz;
            _mapObject.MaxZoom = maxz;
            setOpacity(evtpnl, 1);
        }
        Debug("initMap done");
    };


    this.setupMap = function (epid, minz, maxz) {
        var mapobj = this.GetMap();
        if (mapobj != null) {
            mapobj.MinZoom = minz;
            mapobj.MaxZoom = maxz;
        }
        else {
            alert('no map found - Check egismaptiled.axd handler added to web.config');
        }
    };


    this.setupMapEventHandlers = function (epid, zoomHandler, boundsHandler) {
        var mapobj = this.GetMap();
        if (mapobj != null) {
            if (zoomHandler != null) {
                mapobj.ZoomChangedEvent.subscribe(zoomHandler, mapobj);
            }
            if (boundsHandler != null) {
                mapobj.BoundsChangedEvent.subscribe(boundsHandler, mapobj);
            }
        }
    };


    this.AddWindowLoadEventHandler = function (handlerFunction) {
        if (window.addEventListener) {
            window.addEventListener("load", handlerFunction, false);
        }
        else {
            window.attachEvent("onload", handlerFunction); //ms
        }
    };


    function extend() {
        for (var i = 1; i < arguments.length; i++)
            for (var key in arguments[i])
                if (arguments[i].hasOwnProperty(key))
                    arguments[0][key] = arguments[i][key];
        return arguments[0];
    };

};