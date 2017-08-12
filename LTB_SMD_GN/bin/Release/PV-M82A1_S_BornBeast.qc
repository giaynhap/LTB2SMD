/*
File QC - Duoc tao boi tools Convert LTB to SMD <Author:Giay Nhap>
Fb:fb.com\abcGiayNhapcba
*/

$modelname "PV-M82A1_S_BornBeast.mdl"
$cd "G:\visual studio 2010\Projects\LTB_SMD_GN\LTB_SMD_GN\bin\Release\\"
$cdtexture ".\"
$scale 1.0
$cliptotextures

$bbox 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000
$cbox 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000
$eyeposition 0.000000 0.000000 0.000000
$origin 0.000000 1.500000 -1.000000
$rotate 180.000000 0.000000 0.000000
$scale 0.4

$body "gn_mesh" "PV-M82A1_S_BornBeast" 
$body "gn_mesh" "PV-M82A1_S_BornBeast_1" 
$body "gn_mesh" "PV-M82A1_S_BornBeast_0" 

$sequence "reload" "reload" fps 59
$sequence "select" "select" fps 59
$sequence "run" "run" fps 59 loop
$sequence "idle_0" "idle_0" fps 59 loop
$sequence "fire" "fire" fps 59
$sequence "postfire" "postfire" fps 59
$sequence "prefire" "prefire" fps 59
