﻿// <auto1-generated />
grammar vb;

compilation_unit
  : option_statement* imports_statement* attributes_statement* statement* punctuation
  ;

option_statement
  : 'Option' ('Explicit' | 'Strict' | 'Compare' | 'Infer') ('On' | 'Off' | 'Text' | 'Binary')?
  ;

imports_statement
  : 'Imports' (imports_clause(',' imports_clause)*)?
  ;

imports_clause
  : simple_imports_clause
  | xml_namespace_imports_clause
  ;

simple_imports_clause
  : import_alias_clause? name
  ;

import_alias_clause
  : identifier_token '='
  ;

name
  : cref_operator_reference
  | global_name
  | qualified_cref_operator_reference
  | qualified_name
  | simple_name
  ;

cref_operator_reference
  : 'Operator' ('CType' | 'IsTrue' | 'IsFalse' | 'Not' | '+' | '-' | '*' | '/' | '^' | '\\' | '&' | '<<' | '>>' | 'Mod' | 'Or' | 'Xor' | 'And' | 'Like' | '=' | '<>' | '<' | '<=' | '>=' | '>')
  ;

global_name
  : 'Global'
  ;

qualified_cref_operator_reference
  : name '.' cref_operator_reference
  ;

qualified_name
  : name '.' simple_name
  ;

simple_name
  : generic_name
  | identifier_name
  ;

generic_name
  : identifier_token type_argument_list
  ;

type_argument_list
  : '(' 'Of' (type(',' type)*)? ')'
  ;

type
  : array_type
  | name
  | nullable_type
  | predefined_type
  | tuple_type
  ;

array_type
  : type array_rank_specifier*
  ;

array_rank_specifier
  : '(' ','* ')'
  ;

nullable_type
  : type '?'
  ;

predefined_type
  : ('Object' | 'Boolean' | 'Date' | 'Char' | 'String' | 'Decimal' | 'Byte' | 'SByte' | 'UShort' | 'Short' | 'UInteger' | 'Integer' | 'ULong' | 'Long' | 'Single' | 'Double')
  ;

tuple_type
  : '(' (tuple_element(',' tuple_element)*)? ')'
  ;

tuple_element
  : named_tuple_element
  | typed_tuple_element
  ;

named_tuple_element
  : identifier_token simple_as_clause?
  ;

simple_as_clause
  : 'As' attribute_list* type
  ;

attribute_list
  : '<' (attribute(',' attribute)*)? '>'
  ;

attribute
  : attribute_target? type argument_list?
  ;

attribute_target
  : ('Assembly' | 'Module') ':'
  ;

argument_list
  : '(' (argument(',' argument)*)? ')'
  ;

argument
  : omitted_argument
  | range_argument
  | simple_argument
  ;

omitted_argument
  : punctuation
  ;

punctuation
  : bad_token
  ;

range_argument
  : expression 'To' expression
  ;

expression
  : aggregation
  | await_expression
  | binary_conditional_expression
  | binary_expression
  | cast_expression
  | collection_initializer
  | conditional_access_expression
  | event_container
  | get_type_expression
  | get_xml_namespace_expression
  | instance_expression
  | interpolated_string_expression
  | invocation_expression
  | label
  | lambda_expression
  | literal_expression
  | member_access_expression
  | mid_expression
  | name_of_expression
  | new_expression
  | parenthesized_expression
  | predefined_cast_expression
  | query_expression
  | ternary_conditional_expression
  | tuple_expression
  | type
  | type_of_expression
  | unary_expression
  | xml_member_access_expression
  | xml_node
  ;

aggregation
  : function_aggregation
  | group_aggregation
  ;

function_aggregation
  : identifier_token '('? expression? ')'?
  ;

group_aggregation
  : 'Group'
  ;

await_expression
  : 'Await' expression
  ;

binary_conditional_expression
  : 'If' '(' expression ',' expression ')'
  ;

binary_expression
  : expression ('+' | '-' | '*' | '/' | '\\' | '^' | '<<' | '>>' | '&' | 'Mod' | '=' | '<>' | '<' | '<=' | '>=' | '>' | 'Is' | 'IsNot' | 'Like' | 'Or' | 'Xor' | 'And' | 'OrElse' | 'AndAlso') expression
  ;

cast_expression
  : c_type_expression
  | direct_cast_expression
  | try_cast_expression
  ;

c_type_expression
  : 'CType' '(' expression ',' type ')'
  ;

direct_cast_expression
  : 'DirectCast' '(' expression ',' type ')'
  ;

try_cast_expression
  : 'TryCast' '(' expression ',' type ')'
  ;

collection_initializer
  : '{' (expression(',' expression)*)? '}'
  ;

conditional_access_expression
  : expression? '?' expression
  ;

event_container
  : keyword_event_container
  | with_events_event_container
  | with_events_property_event_container
  ;

keyword_event_container
  : ('MyBase' | 'Me' | 'MyClass')
  ;

with_events_event_container
  : identifier_token
  ;

with_events_property_event_container
  : with_events_event_container '.' identifier_name
  ;

identifier_name
  : identifier_token
  ;

get_type_expression
  : 'GetType' '(' type ')'
  ;

get_xml_namespace_expression
  : 'GetXmlNamespace' '(' xml_prefix_name? ')'
  ;

xml_prefix_name
  : xml_name_token
  ;

instance_expression
  : me_expression
  | my_base_expression
  | my_class_expression
  ;

me_expression
  : 'Me'
  ;

my_base_expression
  : 'MyBase'
  ;

my_class_expression
  : 'MyClass'
  ;

interpolated_string_expression
  : '$"' interpolated_string_content* '"'
  ;

interpolated_string_content
  : interpolated_string_text
  | interpolation
  ;

interpolated_string_text
  : interpolated_string_text_token
  ;

interpolation
  : '{' expression interpolation_alignment_clause? interpolation_format_clause? '}'
  ;

interpolation_alignment_clause
  : ',' expression
  ;

interpolation_format_clause
  : ':' interpolated_string_text_token
  ;

invocation_expression
  : expression? argument_list?
  ;

label
  : (identifier_token | integer_literal_token | 'Next')
  ;

lambda_expression
  : multi_line_lambda_expression
  | single_line_lambda_expression
  ;

multi_line_lambda_expression
  : lambda_header statement* end_block_statement
  ;

lambda_header
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shared' | 'Shadows' | 'Overloads' | 'Overrides' | 'Partial' | 'NotOverridable' | 'Overridable' | 'MustOverride' | 'ReadOnly' | 'WriteOnly' | 'Default' | 'WithEvents' | 'Widening' | 'Narrowing' | 'Custom')* ('Sub' | 'Function') parameter_list? simple_as_clause?
  ;

parameter_list
  : '(' (parameter(',' parameter)*)? ')'
  ;

parameter
  : attribute_list* ('ByVal' | 'ByRef' | 'Optional' | 'ParamArray')* modified_identifier simple_as_clause? equals_value?
  ;

modified_identifier
  : identifier_token '?'? argument_list? array_rank_specifier*
  ;

equals_value
  : '=' expression
  ;

statement
  : case_statement
  | catch_statement
  | declaration_statement
  | do_statement
  | else_if_statement
  | else_statement
  | empty_statement
  | executable_statement
  | finally_statement
  | for_or_for_each_statement
  | if_statement
  | loop_statement
  | next_statement
  | select_statement
  | sync_lock_statement
  | try_statement
  | using_statement
  | while_statement
  | with_statement
  ;

case_statement
  : 'Case' (case_clause(',' case_clause)*)?
  ;

case_clause
  : else_case_clause
  | range_case_clause
  | relational_case_clause
  | simple_case_clause
  ;

else_case_clause
  : 'Else'
  ;

range_case_clause
  : expression 'To' expression
  ;

relational_case_clause
  : 'Is'? ('=' | '<>' | '<' | '<=' | '>=' | '>') expression
  ;

simple_case_clause
  : expression
  ;

catch_statement
  : 'Catch' identifier_name? simple_as_clause? catch_filter_clause?
  ;

catch_filter_clause
  : 'When' expression
  ;

declaration_statement
  : attributes_statement
  | end_block_statement
  | enum_block
  | enum_member_declaration
  | enum_statement
  | event_block
  | field_declaration
  | imports_statement
  | incomplete_member
  | inherits_or_implements_statement
  | method_base
  | method_block_base
  | namespace_block
  | namespace_statement
  | option_statement
  | property_block
  | type_block
  | type_statement
  ;

attributes_statement
  : attribute_list*
  ;

end_block_statement
  : 'End' ('If' | 'Using' | 'With' | 'Select' | 'Structure' | 'Enum' | 'Interface' | 'Class' | 'Module' | 'Namespace' | 'Sub' | 'Function' | 'Get' | 'Set' | 'Property' | 'Operator' | 'Event' | 'AddHandler' | 'RemoveHandler' | 'RaiseEvent' | 'While' | 'Try' | 'SyncLock')
  ;

enum_block
  : enum_statement statement* end_block_statement
  ;

enum_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shadows')* 'Enum' identifier_token as_clause?
  ;

as_clause
  : as_new_clause
  | simple_as_clause
  ;

as_new_clause
  : 'As' new_expression
  ;

new_expression
  : anonymous_object_creation_expression
  | array_creation_expression
  | object_creation_expression
  ;

anonymous_object_creation_expression
  : 'New' attribute_list* object_member_initializer
  ;

object_member_initializer
  : 'With' '{' (field_initializer(',' field_initializer)*)? '}'
  ;

field_initializer
  : inferred_field_initializer
  | named_field_initializer
  ;

inferred_field_initializer
  : 'Key'? expression
  ;

named_field_initializer
  : 'Key'? '.' identifier_name '=' expression
  ;

array_creation_expression
  : 'New' attribute_list* type argument_list? array_rank_specifier* collection_initializer
  ;

object_creation_expression
  : 'New' attribute_list* type argument_list? object_creation_initializer?
  ;

object_creation_initializer
  : object_collection_initializer
  | object_member_initializer
  ;

object_collection_initializer
  : 'From' collection_initializer
  ;

enum_member_declaration
  : attribute_list* identifier_token equals_value?
  ;

event_block
  : event_statement accessor_block* end_block_statement
  ;

event_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shared' | 'Shadows' | 'Overloads' | 'Overrides' | 'Partial' | 'NotOverridable' | 'Overridable' | 'MustOverride' | 'ReadOnly' | 'WriteOnly' | 'Default' | 'WithEvents' | 'Widening' | 'Narrowing' | 'Custom')* 'Custom'? 'Event' identifier_token parameter_list? simple_as_clause? implements_clause?
  ;

implements_clause
  : 'Implements' (qualified_name(',' qualified_name)*)?
  ;

accessor_block
  : accessor_statement statement* end_block_statement
  ;

accessor_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shared' | 'Shadows' | 'Overloads' | 'Overrides' | 'Partial' | 'NotOverridable' | 'Overridable' | 'MustOverride' | 'ReadOnly' | 'WriteOnly' | 'Default' | 'WithEvents' | 'Widening' | 'Narrowing' | 'Custom')* ('Get' | 'Set' | 'AddHandler' | 'RemoveHandler' | 'RaiseEvent') parameter_list?
  ;

field_declaration
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shared' | 'Shadows' | 'ReadOnly' | 'Dim' | 'Const' | 'WithEvents' | 'Widening' | 'Narrowing')* (variable_declarator(',' variable_declarator)*)?
  ;

variable_declarator
  : (modified_identifier(',' modified_identifier)*)? as_clause? equals_value?
  ;

incomplete_member
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shared' | 'Shadows' | 'ReadOnly' | 'Dim' | 'Const' | 'WithEvents' | 'Widening' | 'Narrowing')* identifier_token?
  ;

inherits_or_implements_statement
  : implements_statement
  | inherits_statement
  ;

implements_statement
  : 'Implements' (type(',' type)*)?
  ;

inherits_statement
  : 'Inherits' (type(',' type)*)?
  ;

method_base
  : accessor_statement
  | declare_statement
  | delegate_statement
  | event_statement
  | lambda_header
  | method_statement
  | operator_statement
  | property_statement
  | sub_new_statement
  ;

declare_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shared' | 'Shadows' | 'Overloads' | 'Overrides' | 'Partial' | 'NotOverridable' | 'Overridable' | 'MustOverride' | 'ReadOnly' | 'WriteOnly' | 'Default' | 'WithEvents' | 'Widening' | 'Narrowing' | 'Custom')* 'Declare' ('Ansi' | 'Unicode' | 'Auto')? ('Sub' | 'Function') identifier_token 'Lib' literal_expression 'Alias'? literal_expression? parameter_list? simple_as_clause?
  ;

literal_expression
  : (integer_literal_token | character_literal_token | decimal_literal_token | floating_literal_token | date_literal_token | string_literal_token | 'True' | 'False' | 'Nothing')
  ;

delegate_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shared' | 'Shadows' | 'Overloads' | 'Overrides' | 'Partial' | 'NotOverridable' | 'Overridable' | 'MustOverride' | 'ReadOnly' | 'WriteOnly' | 'Default' | 'WithEvents' | 'Widening' | 'Narrowing' | 'Custom')* 'Delegate' ('Sub' | 'Function') identifier_token type_parameter_list? parameter_list? simple_as_clause?
  ;

type_parameter_list
  : '(' 'Of' (type_parameter(',' type_parameter)*)? ')'
  ;

type_parameter
  : ('In' | 'Out')? identifier_token type_parameter_constraint_clause?
  ;

type_parameter_constraint_clause
  : type_parameter_multiple_constraint_clause
  | type_parameter_single_constraint_clause
  ;

type_parameter_multiple_constraint_clause
  : 'As' '{' (constraint(',' constraint)*)? '}'
  ;

constraint
  : special_constraint
  | type_constraint
  ;

special_constraint
  : ('New' | 'Class' | 'Structure')
  ;

type_constraint
  : type
  ;

type_parameter_single_constraint_clause
  : 'As' constraint
  ;

method_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shared' | 'Shadows' | 'Overloads' | 'Overrides' | 'Partial' | 'NotOverridable' | 'Overridable' | 'MustOverride' | 'ReadOnly' | 'WriteOnly' | 'Default' | 'WithEvents' | 'Widening' | 'Narrowing' | 'Custom')* ('Sub' | 'Function') identifier_token type_parameter_list? parameter_list? simple_as_clause? handles_clause? implements_clause?
  ;

handles_clause
  : 'Handles' (handles_clause_item(',' handles_clause_item)*)?
  ;

handles_clause_item
  : event_container '.' identifier_name
  ;

operator_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shared' | 'Shadows' | 'Overloads' | 'Overrides' | 'Partial' | 'NotOverridable' | 'Overridable' | 'MustOverride' | 'ReadOnly' | 'WriteOnly' | 'Default' | 'WithEvents' | 'Widening' | 'Narrowing' | 'Custom')* 'Operator' ('CType' | 'IsTrue' | 'IsFalse' | 'Not' | '+' | '-' | '*' | '/' | '^' | '\\' | '&' | '<<' | '>>' | 'Mod' | 'Or' | 'Xor' | 'And' | 'Like' | '=' | '<>' | '<' | '<=' | '>=' | '>') parameter_list? simple_as_clause?
  ;

property_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shared' | 'Shadows' | 'Overloads' | 'Overrides' | 'Partial' | 'NotOverridable' | 'Overridable' | 'MustOverride' | 'ReadOnly' | 'WriteOnly' | 'Default' | 'WithEvents' | 'Widening' | 'Narrowing' | 'Custom')* 'Property' identifier_token parameter_list? as_clause? equals_value? implements_clause?
  ;

sub_new_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shared' | 'Shadows' | 'Overloads' | 'Overrides' | 'Partial' | 'NotOverridable' | 'Overridable' | 'MustOverride' | 'ReadOnly' | 'WriteOnly' | 'Default' | 'WithEvents' | 'Widening' | 'Narrowing' | 'Custom')* 'Sub' 'New' parameter_list?
  ;

method_block_base
  : accessor_block
  | constructor_block
  | method_block
  | operator_block
  ;

constructor_block
  : sub_new_statement statement* end_block_statement
  ;

method_block
  : method_statement statement* end_block_statement
  ;

operator_block
  : operator_statement statement* end_block_statement
  ;

namespace_block
  : namespace_statement statement* end_block_statement
  ;

namespace_statement
  : 'Namespace' name
  ;

property_block
  : property_statement accessor_block* end_block_statement
  ;

type_block
  : class_block
  | interface_block
  | module_block
  | structure_block
  ;

class_block
  : class_statement inherits_statement* implements_statement* statement* end_block_statement
  ;

class_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shadows' | 'MustInherit' | 'NotInheritable' | 'Partial')* 'Class' identifier_token type_parameter_list?
  ;

interface_block
  : interface_statement inherits_statement* implements_statement* statement* end_block_statement
  ;

interface_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shadows' | 'MustInherit' | 'NotInheritable' | 'Partial')* 'Interface' identifier_token type_parameter_list?
  ;

module_block
  : module_statement inherits_statement* implements_statement* statement* end_block_statement
  ;

module_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shadows' | 'MustInherit' | 'NotInheritable' | 'Partial')* 'Module' identifier_token type_parameter_list?
  ;

structure_block
  : structure_statement inherits_statement* implements_statement* statement* end_block_statement
  ;

structure_statement
  : attribute_list* ('Public' | 'Private' | 'Protected' | 'Friend' | 'Shadows' | 'MustInherit' | 'NotInheritable' | 'Partial')* 'Structure' identifier_token type_parameter_list?
  ;

type_statement
  : class_statement
  | interface_statement
  | module_statement
  | structure_statement
  ;

do_statement
  : 'Do' while_or_until_clause?
  ;

while_or_until_clause
  : ('While' | 'Until') expression
  ;

else_if_statement
  : 'ElseIf' expression 'Then'?
  ;

else_statement
  : 'Else'
  ;

empty_statement
  : punctuation
  ;

executable_statement
  : add_remove_handler_statement
  | assignment_statement
  | call_statement
  | continue_statement
  | do_loop_block
  | erase_statement
  | error_statement
  | exit_statement
  | expression_statement
  | for_or_for_each_block
  | go_to_statement
  | label_statement
  | local_declaration_statement
  | multi_line_if_block
  | on_error_go_to_statement
  | on_error_resume_next_statement
  | print_statement
  | raise_event_statement
  | re_dim_statement
  | resume_statement
  | return_statement
  | select_block
  | single_line_if_statement
  | stop_or_end_statement
  | sync_lock_block
  | throw_statement
  | try_block
  | using_block
  | while_block
  | with_block
  | yield_statement
  ;

add_remove_handler_statement
  : ('AddHandler' | 'RemoveHandler') expression ',' expression
  ;

assignment_statement
  : expression ('=' | '+=' | '-=' | '*=' | '/=' | '\=' | '^=' | '<<=' | '>>=' | '&=') expression
  ;

call_statement
  : 'Call' expression
  ;

continue_statement
  : 'Continue' ('While' | 'Do' | 'For')
  ;

do_loop_block
  : do_statement statement* loop_statement
  ;

loop_statement
  : 'Loop' while_or_until_clause?
  ;

erase_statement
  : 'Erase' (expression(',' expression)*)?
  ;

error_statement
  : 'Error' expression
  ;

exit_statement
  : 'Exit' ('Do' | 'For' | 'Sub' | 'Function' | 'Operator' | 'Property' | 'Try' | 'Select' | 'While')
  ;

expression_statement
  : expression
  ;

for_or_for_each_block
  : for_block
  | for_each_block
  ;

for_block
  : for_statement statement* next_statement?
  ;

for_statement
  : 'For' visual_basic_syntax_node '=' expression 'To' expression for_step_clause?
  ;

for_step_clause
  : 'Step' expression
  ;

next_statement
  : 'Next' (expression(',' expression)*)?
  ;

for_each_block
  : for_each_statement statement* next_statement?
  ;

for_each_statement
  : 'For' 'Each' visual_basic_syntax_node 'In' expression
  ;

go_to_statement
  : 'GoTo' label
  ;

label_statement
  : (identifier_token | integer_literal_token) ':'
  ;

local_declaration_statement
  : ('Static' | 'Dim' | 'Const')* (variable_declarator(',' variable_declarator)*)?
  ;

multi_line_if_block
  : if_statement statement* else_if_block* else_block? end_block_statement
  ;

if_statement
  : 'If' expression 'Then'?
  ;

else_if_block
  : else_if_statement statement*
  ;

else_block
  : else_statement statement*
  ;

on_error_go_to_statement
  : 'On' 'Error' 'GoTo' '-'? label
  ;

on_error_resume_next_statement
  : 'On' 'Error' 'Resume' 'Next'
  ;

print_statement
  : '?' expression
  ;

raise_event_statement
  : 'RaiseEvent' identifier_name argument_list?
  ;

re_dim_statement
  : 'ReDim' 'Preserve'? (redim_clause(',' redim_clause)*)?
  ;

redim_clause
  : expression argument_list
  ;

resume_statement
  : 'Resume' label?
  ;

return_statement
  : 'Return' expression?
  ;

select_block
  : select_statement case_block* end_block_statement
  ;

select_statement
  : 'Select' 'Case'? expression
  ;

case_block
  : case_statement statement*
  ;

single_line_if_statement
  : 'If' expression 'Then' statement* single_line_else_clause?
  ;

single_line_else_clause
  : 'Else' statement*
  ;

stop_or_end_statement
  : ('Stop' | 'End')
  ;

sync_lock_block
  : sync_lock_statement statement* end_block_statement
  ;

sync_lock_statement
  : 'SyncLock' expression
  ;

throw_statement
  : 'Throw' expression?
  ;

try_block
  : try_statement statement* catch_block* finally_block? end_block_statement
  ;

try_statement
  : 'Try'
  ;

catch_block
  : catch_statement statement*
  ;

finally_block
  : finally_statement statement*
  ;

finally_statement
  : 'Finally'
  ;

using_block
  : using_statement statement* end_block_statement
  ;

using_statement
  : 'Using' expression? (variable_declarator(',' variable_declarator)*)?
  ;

while_block
  : while_statement statement* end_block_statement
  ;

while_statement
  : 'While' expression
  ;

with_block
  : with_statement statement* end_block_statement
  ;

with_statement
  : 'With' expression
  ;

yield_statement
  : 'Yield' expression
  ;

for_or_for_each_statement
  : for_each_statement
  | for_statement
  ;

single_line_lambda_expression
  : lambda_header visual_basic_syntax_node
  ;

member_access_expression
  : expression? ('.' | '!') simple_name
  ;

mid_expression
  : identifier_token argument_list
  ;

name_of_expression
  : 'NameOf' '(' expression ')'
  ;

parenthesized_expression
  : '(' expression ')'
  ;

predefined_cast_expression
  : ('CObj' | 'CBool' | 'CDate' | 'CChar' | 'CStr' | 'CDec' | 'CByte' | 'CSByte' | 'CUShort' | 'CShort' | 'CUInt' | 'CInt' | 'CULng' | 'CLng' | 'CSng' | 'CDbl') '(' expression ')'
  ;

query_expression
  : query_clause*
  ;

query_clause
  : aggregate_clause
  | distinct_clause
  | from_clause
  | group_by_clause
  | join_clause
  | let_clause
  | order_by_clause
  | partition_clause
  | partition_while_clause
  | select_clause
  | where_clause
  ;

aggregate_clause
  : 'Aggregate' (collection_range_variable(',' collection_range_variable)*)? query_clause* 'Into' (aggregation_range_variable(',' aggregation_range_variable)*)?
  ;

collection_range_variable
  : modified_identifier simple_as_clause? 'In' expression
  ;

aggregation_range_variable
  : variable_name_equals? aggregation
  ;

variable_name_equals
  : modified_identifier simple_as_clause? '='
  ;

distinct_clause
  : 'Distinct'
  ;

from_clause
  : 'From' (collection_range_variable(',' collection_range_variable)*)?
  ;

group_by_clause
  : 'Group' (expression_range_variable(',' expression_range_variable)*)? 'By' (expression_range_variable(',' expression_range_variable)*)? 'Into' (aggregation_range_variable(',' aggregation_range_variable)*)?
  ;

expression_range_variable
  : variable_name_equals? expression
  ;

join_clause
  : group_join_clause
  | simple_join_clause
  ;

group_join_clause
  : 'Group' 'Join' (collection_range_variable(',' collection_range_variable)*)? join_clause* 'On' (join_condition('And' join_condition)*)? 'Into' (aggregation_range_variable(',' aggregation_range_variable)*)?
  ;

join_condition
  : expression 'Equals' expression
  ;

simple_join_clause
  : 'Join' (collection_range_variable(',' collection_range_variable)*)? join_clause* 'On' (join_condition('And' join_condition)*)?
  ;

let_clause
  : 'Let' (expression_range_variable(',' expression_range_variable)*)?
  ;

order_by_clause
  : 'Order' 'By' (ordering(',' ordering)*)?
  ;

ordering
  : expression ('Ascending' | 'Descending')?
  ;

partition_clause
  : ('Skip' | 'Take') expression
  ;

partition_while_clause
  : ('Skip' | 'Take') 'While' expression
  ;

select_clause
  : 'Select' (expression_range_variable(',' expression_range_variable)*)?
  ;

where_clause
  : 'Where' expression
  ;

ternary_conditional_expression
  : 'If' '(' expression ',' expression ',' expression ')'
  ;

tuple_expression
  : '(' (simple_argument(',' simple_argument)*)? ')'
  ;

simple_argument
  : name_colon_equals? expression
  ;

name_colon_equals
  : identifier_name ':='
  ;

type_of_expression
  : 'TypeOf' expression ('Is' | 'IsNot') type
  ;

unary_expression
  : ('+' | '-' | 'Not' | 'AddressOf') expression
  ;

xml_member_access_expression
  : expression? '.' ('.' | '@')? '.'? xml_node
  ;

xml_node
  : base_xml_attribute
  | xml_bracketed_name
  | xml_c_data_section
  | xml_comment
  | xml_document
  | xml_element
  | xml_element_end_tag
  | xml_element_start_tag
  | xml_embedded_expression
  | xml_empty_element
  | xml_name
  | xml_prefix_name
  | xml_processing_instruction
  | xml_string
  | xml_text
  ;

base_xml_attribute
  : xml_attribute
  | xml_cref_attribute
  | xml_name_attribute
  ;

xml_attribute
  : xml_node '=' xml_node
  ;

xml_cref_attribute
  : xml_name '=' ('"' | '\'') cref_reference ('"' | '\'')
  ;

xml_name
  : xml_prefix? xml_name_token
  ;

xml_prefix
  : xml_name_token ':'
  ;

cref_reference
  : type cref_signature? simple_as_clause?
  ;

cref_signature
  : '(' (cref_signature_part(',' cref_signature_part)*)? ')'
  ;

cref_signature_part
  : ('ByVal' | 'ByRef')? type?
  ;

xml_name_attribute
  : xml_name '=' ('"' | '\'') identifier_name ('"' | '\'')
  ;

xml_bracketed_name
  : '<' xml_name '>'
  ;

xml_c_data_section
  : '<![CDATA[' xml_text_token* ']]>'
  ;

xml_comment
  : '<!--' xml_text_token* '-->'
  ;

xml_document
  : xml_declaration xml_node* xml_node xml_node*
  ;

xml_declaration
  : '<?' 'xml' xml_declaration_option xml_declaration_option? xml_declaration_option? '?>'
  ;

xml_declaration_option
  : xml_name_token '=' xml_string
  ;

xml_string
  : ('"' | '\'') (xml_text_token)* ('"' | '\'')
  ;

xml_element
  : xml_element_start_tag xml_node* xml_element_end_tag
  ;

xml_element_start_tag
  : '<' xml_node xml_node* '>'
  ;

xml_element_end_tag
  : '</' xml_name? '>'
  ;

xml_embedded_expression
  : '<%=' expression '%>'
  ;

xml_empty_element
  : '<' xml_node xml_node* '/>'
  ;

xml_processing_instruction
  : '<?' xml_name_token xml_text_token* '?>'
  ;

xml_text
  : xml_text_token*
  ;

typed_tuple_element
  : type
  ;

xml_namespace_imports_clause
  : '<' xml_attribute '>'
  ;

structured_trivia
  : directive_trivia
  | documentation_comment_trivia
  | skipped_tokens_trivia
  ;

directive_trivia
  : bad_directive_trivia
  | const_directive_trivia
  | disable_warning_directive_trivia
  | else_directive_trivia
  | enable_warning_directive_trivia
  | end_external_source_directive_trivia
  | end_if_directive_trivia
  | end_region_directive_trivia
  | external_checksum_directive_trivia
  | external_source_directive_trivia
  | if_directive_trivia
  | reference_directive_trivia
  | region_directive_trivia
  ;

bad_directive_trivia
  : '#'
  ;

const_directive_trivia
  : '#' 'Const' identifier_token '=' expression
  ;

disable_warning_directive_trivia
  : '#' 'Disable' 'Warning' (identifier_name(',' identifier_name)*)?
  ;

else_directive_trivia
  : '#' 'Else'
  ;

enable_warning_directive_trivia
  : '#' 'Enable' 'Warning' (identifier_name(',' identifier_name)*)?
  ;

end_external_source_directive_trivia
  : '#' 'End' 'ExternalSource'
  ;

end_if_directive_trivia
  : '#' 'End' 'If'
  ;

end_region_directive_trivia
  : '#' 'End' 'Region'
  ;

external_checksum_directive_trivia
  : '#' 'ExternalChecksum' '(' string_literal_token ',' string_literal_token ',' string_literal_token ')'
  ;

external_source_directive_trivia
  : '#' 'ExternalSource' '(' string_literal_token ',' integer_literal_token ')'
  ;

if_directive_trivia
  : '#' 'Else'? ('If' | 'ElseIf') expression 'Then'?
  ;

reference_directive_trivia
  : '#' 'R' string_literal_token
  ;

region_directive_trivia
  : '#' 'Region' string_literal_token
  ;

documentation_comment_trivia
  : xml_node*
  ;

skipped_tokens_trivia
  : ('AddHandler' | 'AddressOf' | 'Alias' | 'And' | 'AndAlso' | 'As' | 'Boolean' | 'ByRef' | 'Byte' | 'ByVal' | 'Call' | 'Case' | 'Catch' | 'CBool' | 'CByte' | 'CChar' | 'CDate' | 'CDec' | 'CDbl' | 'Char' | 'CInt' | 'Class' | 'CLng' | 'CObj' | 'Const' | 'R' | 'Continue' | 'CSByte' | 'CShort' | 'CSng' | 'CStr' | 'CType' | 'CUInt' | 'CULng' | 'CUShort' | 'Date' | 'Decimal' | 'Declare' | 'Default' | 'Delegate' | 'Dim' | 'DirectCast' | 'Do' | 'Double' | 'Each' | 'Else' | 'ElseIf' | 'End' | 'Enum' | 'Erase' | 'Error' | 'Event' | 'Exit' | 'False' | 'Finally' | 'For' | 'Friend' | 'Function' | 'Get' | 'GetType' | 'GetXmlNamespace' | 'Global' | 'GoTo' | 'Handles' | 'If' | 'Implements' | 'Imports' | 'In' | 'Inherits' | 'Integer' | 'Interface' | 'Is' | 'IsNot' | 'Let' | 'Lib' | 'Like' | 'Long' | 'Loop' | 'Me' | 'Mod' | 'Module' | 'MustInherit' | 'MustOverride' | 'MyBase' | 'MyClass' | 'NameOf' | 'Namespace' | 'Narrowing' | 'Next' | 'New' | 'Not' | 'Nothing' | 'NotInheritable' | 'NotOverridable' | 'Object' | 'Of' | 'On' | 'Operator' | 'Option' | 'Optional' | 'Or' | 'OrElse' | 'Overloads' | 'Overridable' | 'Overrides' | 'ParamArray' | 'Partial' | 'Private' | 'Property' | 'Protected' | 'Public' | 'RaiseEvent' | 'ReadOnly' | 'ReDim' | 'REM' | 'RemoveHandler' | 'Resume' | 'Return' | 'SByte' | 'Select' | 'Set' | 'Shadows' | 'Shared' | 'Short' | 'Single' | 'Static' | 'Step' | 'Stop' | 'String' | 'Structure' | 'Sub' | 'SyncLock' | 'Then' | 'Throw' | 'To' | 'True' | 'Try' | 'TryCast' | 'TypeOf' | 'UInteger' | 'ULong' | 'UShort' | 'Using' | 'When' | 'While' | 'Widening' | 'With' | 'WithEvents' | 'WriteOnly' | 'Xor' | 'EndIf' | 'Gosub' | 'Variant' | 'Wend' | 'Aggregate' | 'All' | 'Ansi' | 'Ascending' | 'Assembly' | 'Auto' | 'Binary' | 'By' | 'Compare' | 'Custom' | 'Descending' | 'Disable' | 'Distinct' | 'Enable' | 'Equals' | 'Explicit' | 'ExternalSource' | 'ExternalChecksum' | 'From' | 'Group' | 'Infer' | 'Into' | 'IsFalse' | 'IsTrue' | 'Join' | 'Key' | 'Mid' | 'Off' | 'Order' | 'Out' | 'Preserve' | 'Region' | 'Skip' | 'Strict' | 'Take' | 'Text' | 'Unicode' | 'Until' | 'Warning' | 'Where' | 'Type' | 'xml' | 'Async' | 'Await' | 'Iterator' | 'Yield' | '!' | '@' | ',' | '#' | '&' | '\'' | '(' | ')' | '{' | '}' | ';' | '*' | '+' | '-' | '.' | '/' | ':' | '<' | '<=' | '<>' | '=' | '>' | '>=' | '\\' | '^' | ':=' | '&=' | '*=' | '+=' | '-=' | '/=' | '\=' | '^=' | '<<' | '>>' | '<<=' | '>>=' | '?' | '"' | '$"' | punctuation | 'vbCrLf' | '/>' | '</' | '<!--' | '-->' | '<?' | '?>' | '<%=' | '%>' | '<![CDATA[' | ']]>' | bad_token | xml_name_token | xml_text_token | interpolated_string_text_token | identifier_token | integer_literal_token | floating_literal_token | decimal_literal_token | date_literal_token | string_literal_token | character_literal_token)*
  ;

syntax_token
  : character_literal_token
  | date_literal_token
  | decimal_literal_token
  | floating_literal_token
  | identifier_token
  | integer_literal_token
  | interpolated_string_text_token
  | keyword
  | punctuation
  | string_literal_token
  | xml_name_token
  | xml_text_token
  ;
