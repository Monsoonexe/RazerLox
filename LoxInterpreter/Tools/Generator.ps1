#TODO - handle indenting. For now just use VS's CodeCleanup tool to do so.

Param
(
	[String]
	$OutputDirectory = ""
)

Function DefineType
{
	Param 
	(
		[System.IO.StreamWriter]
		$writer, 

		[String]
		$baseName,

		[String]
		$className,

		[String]
		$fieldList
	)

	# declaration
	$writer.WriteLine("public sealed class $className : $baseName");
	$writer.WriteLine("{");

	# default constructor
	if ($fieldList.Length -lt 5)
	{
		$writer.WriteLine("");
		$writer.WriteLine("public $className() { }");
	}
	else # add fields and constructor with parameters
	{
		# fields declarations
		foreach ($field in $fieldList.Split(","))
		{
			$f = $field.Trim()
			$writer.WriteLine("public readonly $f;");
		}

		$writer.WriteLine("");

		# constructor
		$writer.WriteLine("public $className($fieldList)");
		$writer.WriteLine("{");

		# assignment
		foreach ($field in $fieldList.Split(","))
		{
			$fieldName = $field.Trim().Split(" ")[1];
			$writer.WriteLine("this.$fieldName = $fieldName;");
		}
		$writer.WriteLine("}"); # close constructor
	}
	
	# override Accept
	$writer.WriteLine("");
	$writer.WriteLine("public override T Accept<T>(IVisitor<T> visitor)");
	$writer.WriteLine("{"); # open Accept
	$writer.WriteLine("return visitor.Visit$className(this);");
	$writer.WriteLine("}") # close Accpet

	$writer.WriteLine("}"); # close class
}

Function DefineVisitorInterface
{
	Param
	(
		[System.IO.StreamWriter]
		$writer,

		[String]
		$baseType,

		[String[]]
		$types
	)

	# body
	$writer.WriteLine("public interface IVisitor<T>");
	$writer.WriteLine("{");

	# define members for each type
	foreach ($t in $types)
	{
		$typeName = $t.Split(":")[0].Trim();

		$parameterName = $baseType.ToLower().Substring(1); # AExpression -> expression
		#camelCase parameter name (binaryExpression)
		# $parameterName =  [System.Char]::ToLowerInvariant($typeName[0]) + $typeName.Substring(1);

		# T Visit[TypeName][Base]([TypeName] [typeName]);
		$writer.WriteLine("T Visit$typeName($typeName $parameterName);")
		$writer.WriteLine("");
	}

	$writer.WriteLine("}");
}

Function DefineFile
{
	Param
	(
		[String]
		$outDir,

		[String]
		$baseName,

		[String[]]
		$types
	)

	$outFile = "$outDir\$baseName.cs";

	$stream = [System.IO.File]::Open($outFile, [System.IO.FileMode]::Create, [System.IO.FileShare]::ReadWrite);
	$writer = New-Object System.IO.StreamWriter $stream;
	
	# programmer-facing header
	$writer.WriteLine("/* This file is autogenerated by Generator.ps1.");
	$writer.WriteLine("*  Any changes made to it may be lost the next time it is run.");
	$writer.WriteLine("*/`r`n");
	$writer.WriteLine("using System.Collections.Generic;");
	$writer.WriteLine("");

	$writer.WriteLine("namespace LoxInterpreter.RazerLox");
	$writer.WriteLine("{");
	$writer.WriteLine("public abstract class $baseName");
	$writer.WriteLine("{");

	# visitor pattern
	DefineVisitorInterface $writer $baseName $types
	$writer.WriteLine("public abstract T Accept<T>(IVisitor<T> visitor);");
	$writer.WriteLine("}");

	# SUBCLASSES
	foreach ($type in $types)
	{
		$sub = $type.Split(":");
		$className = $sub[0].Trim();
		$fields = $sub[1].Trim();
		DefineType $writer $baseName $className $fields
	}

	# end namespace
	$writer.WriteLine("}");
	
	# clean up
	$writer.Close();
	$stream.Close();
}

$ErrorActionPreference = 'Stop';

if ([System.String]::IsNullOrEmpty($OutputDirectory))
{
	Set-Location $PSScriptRoot;
	Set-Location ..
	$OutputDirectory = Get-Location;
	$OutputDirectory = "$OutputDirectory/RazerLox"
}

$expressions = 
	  "AssignmentExpression : Token identifier, AExpression value",
      "BinaryExpression   : AExpression left, Token op, AExpression right",
      "GroupingExpression : AExpression expression",
      "LiteralExpression  : object value",
      "UnaryExpression    : Token op, AExpression right",
	  "VariableExpression : Token identifier",
	  "ExitExpression	  : "; # TODO - figure out no-parameter expressions (syscalls)

DefineFile $OutputDirectory "AExpression" $expressions;

$statements = 
	"BlockStatement			: IList<AStatement> statements",
	"ExpressionStatement  	: AExpression expression",
	"IfStatement			: AExpression condition, AStatement thenBranch, AStatement elseBranch",
	"PrintStatement			: AExpression expression",
	"VariableStatement		: Token identifier, AExpression initializer";

DefineFile $OutputDirectory "AStatement" $statements;