﻿#TODO - handle indenting. For now just use VS's CodeCleanup tool to do so.

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

# don't continue if an error happens anywhere in this script
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
      "BinaryExpression   : AExpression left, Token _operator, AExpression right",
	  "CallExpression		: AExpression callee, Token paren, IList<AExpression> args",
	  "GetExpression		: AExpression instance, Token identifier",
      "GroupingExpression : AExpression expression",
      "LiteralExpression  : object value",
	  "LogicalExpression  : AExpression left, Token _operator, AExpression right",
	  "SetExpression		: AExpression instance, Token identifier, AExpression value",
	  "ThisExpression		: Token keyword",
      "UnaryExpression    : Token _operator, AExpression right",
	  "VariableExpression : Token identifier";

DefineFile $OutputDirectory "AExpression" $expressions;

$statements = 
	"BreakStatement			: Token token",
	"BlockStatement			: IList<AStatement> statements",
	"ClassDeclaration		: Token identifier, VariableExpression superclass, IList<FunctionDeclaration> methods",
	"ExpressionStatement  	: AExpression expression",
	"FunctionDeclaration		: Token identifier, IList<Token> parameters, IList<AStatement> body",
	"IfStatement			: AExpression condition, AStatement thenBranch, AStatement elseBranch",
	"PrintStatement			: AExpression expression",
	"ReturnStatement		: Token keyword, AExpression value",
	"VariableDeclaration		: Token identifier, AExpression initializer",
	"WhileStatement			: AExpression condition, AStatement body";

DefineFile $OutputDirectory "AStatement" $statements;